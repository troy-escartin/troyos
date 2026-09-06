// Scoped JS module for the Prompt Builder feature. Used only where Blazor genuinely can't do the
// job itself: clipboard write, auto-scroll to bottom, and textarea auto-resize. No globals are
// created — everything is an ES module export, imported once per component via IJSObjectReference.

const enterListeners = new WeakMap();

export function scrollToBottom(element) {
    if (!element) {
        return;
    }

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    element.scrollTo({ top: element.scrollHeight, behavior: prefersReducedMotion ? 'auto' : 'smooth' });
}

export function autoResizeTextArea(element) {
    if (!element) {
        return;
    }

    element.style.height = 'auto';
    element.style.height = `${element.scrollHeight}px`;
}

export function focusElement(element) {
    if (element && typeof element.focus === 'function') {
        element.focus();
    }
}

export async function copyText(text) {
    if (typeof text !== 'string' || text.length === 0) {
        return false;
    }

    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch {
        // Permission denied or unavailable — fall through to the legacy fallback below.
    }

    try {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.setAttribute('readonly', '');
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();
        const successful = document.execCommand('copy');
        document.body.removeChild(textarea);
        return successful;
    } catch {
        return false;
    }
}

// Enter-to-send needs a native listener so we can conditionally preventDefault based on the
// Shift key state at the moment the key is pressed — Blazor's declarative preventDefault can't
// make that decision per-keystroke.
export function bindEnterToSend(element, dotNetRef) {
    if (!element) {
        return;
    }

    unbindEnterToSend(element);

    const handler = (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            dotNetRef.invokeMethodAsync('HandleEnterSubmit');
        }
    };

    element.addEventListener('keydown', handler);
    enterListeners.set(element, handler);
}

export function unbindEnterToSend(element) {
    if (!element) {
        return;
    }

    const handler = enterListeners.get(element);
    if (handler) {
        element.removeEventListener('keydown', handler);
        enterListeners.delete(element);
    }
}
