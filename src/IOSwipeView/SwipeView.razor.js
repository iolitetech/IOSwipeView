/*
 * The renderer for SwipeView.
 *
 * The split with C# is deliberate: C# owns the state machine and decides *where* the row should
 * be, this module decides *how it looks* at a given offset. That keeps the maths in one tested
 * place while letting the settle animation run entirely in the browser, with no interop per frame.
 */

const instances = new Map();
let nextHandle = 0;

const reducedMotion = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)');

/**
 * Attaches the renderer to a row.
 *
 * @param {HTMLElement} root the row's root element
 * @param {object} options spacing and fade points, mirrored from SwipeOptions
 * @param {object} dotNetRef the owning SwipeView, for resize callbacks
 * @returns {number} a handle used by every other export
 */
export function create(root, options, dotNetRef) {
    const handle = nextHandle++;

    const isRtl = (root.closest('[dir="rtl"]') !== null) || (getComputedStyle(root).direction === 'rtl');

    const instance = {
        root,
        options,
        dotNetRef,
        offset: 0,
        frame: 0,
        observer: null,
        mutationObserver: null,
        isRtl,
        width: root.clientWidth,
    };

    const updateDirectionAndSize = () => {
        const width = root.clientWidth;
        const currentRtl = (root.closest('[dir="rtl"]') !== null) || (getComputedStyle(root).direction === 'rtl');
        if (instance.isRtl !== currentRtl || Math.abs(instance.width - width) > 1) {
            instance.isRtl = currentRtl;
            instance.width = width;
            dotNetRef.invokeMethodAsync('OnRowResized', width, currentRtl);
            render(instance, instance.offset);
        }
    };

    // The triggered offset has to carry the row fully off-screen, so C# needs its live width.
    if (globalThis.ResizeObserver) {
        instance.observer = new ResizeObserver((entries) => {
            const width = entries[0]?.contentRect?.width ?? root.clientWidth;
            const currentRtl = (root.closest('[dir="rtl"]') !== null) || (getComputedStyle(root).direction === 'rtl');
            instance.isRtl = currentRtl;
            instance.width = width;
            dotNetRef.invokeMethodAsync('OnRowResized', width, currentRtl);
        });
        instance.observer.observe(root);
    } else {
        dotNetRef.invokeMethodAsync('OnRowResized', root.clientWidth, isRtl);
    }

    if (globalThis.MutationObserver) {
        instance.mutationObserver = new MutationObserver(updateDirectionAndSize);
        instance.mutationObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['dir', 'class', 'style'],
            subtree: true,
        });
    }

    instances.set(handle, instance);
    applyCascadeZIndices(root);
    render(instance, 0);

    return handle;
}

/**
 * Replaces the options, for example after actions are added or removed.
 *
 * @param {number} handle the instance handle
 * @param {object} options the new options
 */
export function setOptions(handle, options) {
    const instance = instances.get(handle);
    if (!instance) return;

    instance.options = options;
    applyCascadeZIndices(instance.root);
    render(instance, instance.offset);
}

function applyCascadeZIndices(root) {
    if (!root.classList.contains('ioswipe--cascade')) return;
    const leading = root.querySelectorAll('.ioswipe__actions--leading .ioswipe__action-slot');
    leading.forEach((slot, i) => {
        slot.style.zIndex = `${leading.length - i}`;
    });
    const trailing = root.querySelectorAll('.ioswipe__actions--trailing .ioswipe__action-slot');
    trailing.forEach((slot, i) => {
        slot.style.zIndex = `${i + 1}`;
    });
}

/**
 * Moves the row immediately. Called once per pointer move while dragging.
 *
 * @param {number} handle the instance handle
 * @param {number} offset the signed offset in pixels
 */
export function setOffset(handle, offset) {
    const instance = instances.get(handle);
    if (!instance) return;

    cancelAnimationFrame(instance.frame);
    instance.frame = 0;
    instance.root.classList.add('ioswipe--dragging');
    render(instance, offset);
}

/**
 * Marks a side as armed for drag-to-trigger, and vibrates on the transition.
 *
 * @param {number} handle the instance handle
 * @param {string|null} side 'leading', 'trailing', or null to disarm
 * @param {boolean} haptics whether to vibrate
 */
export function setArmed(handle, side, haptics, pattern, armedIndex) {
    const instance = instances.get(handle);
    if (!instance) return;

    const { classList } = instance.root;
    classList.toggle('ioswipe--armed-leading', side === 'leading');
    classList.toggle('ioswipe--armed-trailing', side === 'trailing');

    const strips = [
        { el: instance.root.querySelector('.ioswipe__actions--leading'), activeSide: 'leading' },
        { el: instance.root.querySelector('.ioswipe__actions--trailing'), activeSide: 'trailing' },
    ];

    for (const { el, activeSide } of strips) {
        if (!el) continue;
        const slots = el.querySelectorAll('.ioswipe__action-slot');
        slots.forEach((slot, i) => {
            const isArmedSlot = side === activeSide && i === armedIndex;
            const isSiblingCollapsed = side === activeSide && i !== armedIndex;
            slot.classList.toggle('ioswipe__action-slot--armed', isArmedSlot);
            slot.classList.toggle('ioswipe__action-slot--collapsed', isSiblingCollapsed);
        });
    }

    // Only honoured on devices with a motor, and only after a user gesture. A no-op elsewhere.
    if (haptics && side) {
        navigator.vibrate?.(pattern || 10);
    }
}

/**
 * Springs the row to its resting position.
 *
 * @param {number} handle the instance handle
 * @param {number} to the target offset in pixels
 * @param {number} stiffness the spring constant
 * @param {number} damping the damping coefficient
 * @param {number} velocity the release velocity in pixels per second
 */
export function settle(handle, to, stiffness, damping, velocity) {
    const instance = instances.get(handle);
    if (!instance) return;

    cancelAnimationFrame(instance.frame);
    instance.root.classList.remove('ioswipe--dragging');

    if (reducedMotion?.matches) {
        instance.frame = 0;
        render(instance, to);
        return;
    }

    // Displacement from the target, so the spring solves towards zero.
    let x = instance.offset - to;
    let v = velocity;
    let previous = performance.now();

    const step = (now) => {
        // Clamp the frame delta so a backgrounded tab does not integrate one enormous step.
        let remaining = Math.min((now - previous) / 1000, 0.064);
        previous = now;

        // Fixed sub-steps keep a stiff spring stable regardless of the display's refresh rate.
        const h = 1 / 240;
        while (remaining > 0) {
            const dt = Math.min(h, remaining);
            remaining -= dt;

            const acceleration = (-stiffness * x) - (damping * v);
            v += acceleration * dt;
            x += v * dt;
        }

        // Settled: within half a pixel and no longer moving perceptibly.
        if (Math.abs(x) < 0.5 && Math.abs(v) < 5) {
            instance.frame = 0;
            render(instance, to);
            return;
        }

        render(instance, to + x);
        instance.frame = requestAnimationFrame(step);
    };

    instance.frame = requestAnimationFrame(step);
}

/**
 * Detaches the renderer and releases its observer.
 *
 * @param {number} handle the instance handle
 */
export function dispose(handle) {
    const instance = instances.get(handle);
    if (!instance) return;

    cancelAnimationFrame(instance.frame);
    instance.observer?.disconnect();
    instance.mutationObserver?.disconnect();
    instances.delete(handle);
}

/**
 * Writes the custom properties every visual aspect of the row derives from.
 *
 * @param {object} instance the instance record
 * @param {number} offset the signed offset in pixels
 */
function render(instance, offset) {
    const { root, options } = instance;
    instance.offset = offset;

    const isRtl = (root.closest('[dir="rtl"]') !== null) || (getComputedStyle(root).direction === 'rtl');
    if (instance.isRtl !== isRtl) {
        instance.isRtl = isRtl;
        instance.dotNetRef?.invokeMethodAsync('OnRowResized', instance.width || root.clientWidth, isRtl);
    }

    // Under RTL, positive logical offset (leading revealed on the right) translates physical content left (-X),
    // and negative logical offset (trailing revealed on the left) translates physical content right (+X).
    const physicalOffset = isRtl ? -offset : offset;

    root.style.setProperty('--ioswipe-offset', `${physicalOffset.toFixed(2)}px`);

    writeSide(root, options, offset, 'leading', 1, options.leadingCount);
    writeSide(root, options, offset, 'trailing', -1, options.trailingCount);
}

function writeSide(root, options, offset, name, sign, count) {
    const dragged = offset * sign;

    // How much of this side's strip is uncovered, never negative.
    const visible = Math.max(0, dragged - options.spacing);

    // The strip's natural width, so the mask style can hold the actions still beneath the row.
    const natural = count > 0
        ? (count * options.actionWidth) + ((count - 1) * options.spacing)
        : 0;

    const opacity = opacityFor(options, dragged);

    root.style.setProperty(`--ioswipe-${name}-width`, `${visible.toFixed(2)}px`);
    root.style.setProperty(`--ioswipe-${name}-natural-width`, `${natural.toFixed(2)}px`);
    root.style.setProperty(`--ioswipe-${name}-opacity`, opacity.toFixed(3));

    const sideEl = root.querySelector(`.ioswipe__actions--${name}`);
    if (sideEl) {
        const isVisible = visible > 0.1;
        sideEl.style.visibility = isVisible ? 'visible' : 'hidden';
        sideEl.style.pointerEvents = isVisible ? 'auto' : 'none';
    }

    if (dragged > 0) {
        root.style.setProperty('--ioswipe-current-opacity', opacity.toFixed(3));
    }
}

function opacityFor(options, dragged) {
    const beyondStart = Math.max(0, dragged - options.actionsVisibleStartPoint);
    const range = options.actionsVisibleEndPoint - options.actionsVisibleStartPoint;

    // Matches SwipeGeometry.ActionsOpacity, including its guard: a zero-width range means
    // "no fade" rather than a division by zero.
    if (range <= 0) {
        return beyondStart > 0 ? 1 : 0;
    }

    return Math.min(1, Math.max(0, beyondStart / range));
}
