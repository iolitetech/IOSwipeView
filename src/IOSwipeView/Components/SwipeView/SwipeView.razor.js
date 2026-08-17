/*
 * The TypeScript renderer for IOSwipeView.
 *
 * The architectural split with C# is deliberate:
 * - C# owns the state machine and gesture thresholds (decides WHERE the row should be).
 * - This module decides HOW it looks at a given offset and executes 60/120fps spring physics.
 *
 * This keeps maths in one tested place while allowing settle animations and drag transforms
 * to run entirely in the browser with zero JS-to-C# interop per frame.
 */
const instances = new Map();
let nextHandle = 0;
const reducedMotionQuery = typeof globalThis.matchMedia === 'function'
    ? globalThis.matchMedia('(prefers-reduced-motion: reduce)')
    : null;
/**
 * Attaches the high-performance renderer to a SwipeView row.
 *
 * @param root The row's root HTML element.
 * @param options Spacing, action widths, and fade points mirrored from SwipeOptions.
 * @param dotNetRef The owning C# SwipeView component reference for resize and RTL callbacks.
 * @returns An integer handle used by all subsequent exports.
 */
export function create(root, options, dotNetRef) {
    const handle = nextHandle++;
    const isRtl = isRtlElement(root);
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
        const currentRtl = isRtlElement(root);
        if (instance.isRtl !== currentRtl || Math.abs(instance.width - width) > 1) {
            instance.isRtl = currentRtl;
            instance.width = width;
            void dotNetRef.invokeMethodAsync('OnRowResized', width, currentRtl);
            render(instance, instance.offset);
        }
    };
    // The triggered offset must carry the row fully off-screen, so C# requires live width metrics.
    if (typeof globalThis.ResizeObserver === 'function') {
        instance.observer = new ResizeObserver((entries) => {
            const entry = entries[0];
            const width = entry?.contentRect?.width ?? root.clientWidth;
            const currentRtl = isRtlElement(root);
            instance.isRtl = currentRtl;
            instance.width = width;
            void dotNetRef.invokeMethodAsync('OnRowResized', width, currentRtl);
        });
        instance.observer.observe(root);
    }
    else {
        void dotNetRef.invokeMethodAsync('OnRowResized', root.clientWidth, isRtl);
    }
    if (typeof globalThis.MutationObserver === 'function') {
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
 * Replaces the options (e.g. after actions are dynamically added or removed).
 *
 * @param handle The instance handle.
 * @param options The new SwipeOptions.
 */
export function setOptions(handle, options) {
    const instance = instances.get(handle);
    if (!instance)
        return;
    instance.options = options;
    applyCascadeZIndices(instance.root);
    render(instance, instance.offset);
}
/**
 * Moves the row immediately. Called once per pointer move frame while dragging.
 *
 * @param handle The instance handle.
 * @param offset The signed offset in CSS pixels.
 */
export function setOffset(handle, offset) {
    const instance = instances.get(handle);
    if (!instance)
        return;
    cancelAnimationFrame(instance.frame);
    instance.frame = 0;
    instance.root.classList.add('ioswipe--dragging');
    render(instance, offset);
}
/**
 * Marks a side as armed for drag-to-trigger, triggering haptic vibration if enabled.
 *
 * @param handle The instance handle.
 * @param side 'leading', 'trailing', or null to disarm.
 * @param haptics Whether to vibrate the device.
 * @param pattern Custom vibration pattern duration in milliseconds.
 * @param armedIndex The index of the specific action slot being armed.
 */
export function setArmed(handle, side, haptics, pattern, armedIndex) {
    const instance = instances.get(handle);
    if (!instance)
        return;
    const { classList } = instance.root;
    classList.toggle('ioswipe--armed-leading', side === 'leading');
    classList.toggle('ioswipe--armed-trailing', side === 'trailing');
    const strips = [
        { el: instance.root.querySelector('.ioswipe__actions--leading'), activeSide: 'leading' },
        { el: instance.root.querySelector('.ioswipe__actions--trailing'), activeSide: 'trailing' },
    ];
    for (const { el, activeSide } of strips) {
        if (!el)
            continue;
        const slots = el.querySelectorAll('.ioswipe__action-slot');
        slots.forEach((slot, i) => {
            const isArmedSlot = side === activeSide && i === armedIndex;
            const isSiblingCollapsed = side === activeSide && i !== armedIndex;
            slot.classList.toggle('ioswipe__action-slot--armed', isArmedSlot);
            slot.classList.toggle('ioswipe__action-slot--collapsed', isSiblingCollapsed);
        });
    }
    // Only honoured on devices with a vibration motor after user gesture.
    if (haptics && side) {
        try {
            navigator.vibrate?.(pattern ?? 10);
        }
        catch {
            // Silently ignore if vibration is restricted by browser policy
        }
    }
}
/**
 * Springs the row to its resting position using an analytical ODE spring solver.
 *
 * @param handle The instance handle.
 * @param to The target offset in CSS pixels.
 * @param stiffness The spring stiffness constant (k).
 * @param damping The spring damping coefficient (c).
 * @param velocity The release velocity in pixels per second.
 */
export function settle(handle, to, stiffness, damping, velocity) {
    const instance = instances.get(handle);
    if (!instance)
        return;
    cancelAnimationFrame(instance.frame);
    instance.root.classList.remove('ioswipe--dragging');
    if (reducedMotionQuery?.matches) {
        instance.frame = 0;
        render(instance, to);
        return;
    }
    // Displacement from target so spring solves towards zero
    let x = instance.offset - to;
    let v = velocity;
    let previous = performance.now();
    const step = (now) => {
        // Clamp frame delta so backgrounded tabs don't integrate one giant step
        let remaining = Math.min((now - previous) / 1000, 0.064);
        previous = now;
        // Fixed sub-step integration (240Hz) ensures stiff springs remain stable
        const h = 1 / 240;
        while (remaining > 0) {
            const dt = Math.min(h, remaining);
            remaining -= dt;
            const acceleration = (-stiffness * x) - (damping * v);
            v += acceleration * dt;
            x += v * dt;
        }
        // Settled condition: within 0.5px and moving slower than 5px/s
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
 * Detaches the renderer and disconnects all observers.
 *
 * @param handle The instance handle.
 */
export function dispose(handle) {
    const instance = instances.get(handle);
    if (!instance)
        return;
    cancelAnimationFrame(instance.frame);
    instance.observer?.disconnect();
    instance.mutationObserver?.disconnect();
    instances.delete(handle);
}
/**
 * Writes the CSS custom properties that all visual aspects of the row derive from.
 */
function render(instance, offset) {
    const { root, options } = instance;
    instance.offset = offset;
    const isRtl = isRtlElement(root);
    if (instance.isRtl !== isRtl) {
        instance.isRtl = isRtl;
        void instance.dotNetRef?.invokeMethodAsync('OnRowResized', instance.width || root.clientWidth, isRtl);
    }
    // Under RTL, positive logical offset (leading revealed on right) translates physical content left (-X),
    // and negative logical offset (trailing revealed on left) translates physical content right (+X).
    const physicalOffset = isRtl ? -offset : offset;
    root.style.setProperty('--ioswipe-offset', `${physicalOffset.toFixed(2)}px`);
    writeSide(root, options, offset, 'leading', 1, options.leadingCount);
    writeSide(root, options, offset, 'trailing', -1, options.trailingCount);
}
function writeSide(root, options, offset, name, sign, count) {
    const dragged = offset * sign;
    // How much of this side's strip is uncovered, never negative
    const visible = Math.max(0, dragged - options.spacing);
    // Natural width so the mask style can hold actions still beneath the row
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
    // Guard: zero-width range means "no fade" rather than division by zero
    if (range <= 0) {
        return beyondStart > 0 ? 1 : 0;
    }
    return Math.min(1, Math.max(0, beyondStart / range));
}
function applyCascadeZIndices(root) {
    if (!root.classList.contains('ioswipe--cascade'))
        return;
    const leading = root.querySelectorAll('.ioswipe__actions--leading .ioswipe__action-slot');
    leading.forEach((slot, i) => {
        slot.style.zIndex = `${leading.length - i}`;
    });
    const trailing = root.querySelectorAll('.ioswipe__actions--trailing .ioswipe__action-slot');
    trailing.forEach((slot, i) => {
        slot.style.zIndex = `${i + 1}`;
    });
}
function isRtlElement(element) {
    return (element.closest('[dir="rtl"]') !== null) || (getComputedStyle(element).direction === 'rtl');
}
