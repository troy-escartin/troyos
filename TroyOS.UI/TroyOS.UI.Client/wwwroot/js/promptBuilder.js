// Scoped JS module for the Prompt Builder feature. Used only where Blazor genuinely can't do the
// job itself: clipboard write, auto-scroll to bottom, and textarea auto-resize. No globals are
// created — everything is an ES module export, imported once per component via IJSObjectReference.

const enterListeners = new WeakMap();

// Local experiment only. Replace this value locally and do not commit or deploy it.
const GEMINI_API_KEY = 'AQ.Ab8RN6LDYmcJMjES-McpFRpKmPbfX4MjKPdioce6YjswK5_Mlw';
const GEMINI_ENDPOINT = 'https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent';

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

export async function refinePrompt(prompt) {
    if (typeof prompt !== 'string' || prompt.trim().length === 0) {
        throw new Error('There is no prompt to refine.');
    }

    if (GEMINI_API_KEY === 'PASTE_LOCAL_GEMINI_KEY_HERE') {
        throw new Error('Add a local Gemini API key in promptBuilder.js first.');
    }

    const response = await fetch(GEMINI_ENDPOINT, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-goog-api-key': GEMINI_API_KEY
        },
        body: JSON.stringify({
            contents: [{
                parts: [{
                    text: [
                        'Act as an expert prompt engineer.',
                        'Improve and complete the prompt below while preserving the user intent and all important facts.',
                        'Make it clear, specific, actionable, and easy for an AI assistant to follow.',
                        'Return only the improved prompt, with no explanation or preamble.',
                        '',
                        'Prompt to improve:',
                        prompt.trim()
                    ].join('\n')
                }]
            }]
        })
    });

    const data = await response.json().catch(() => null);
    if (!response.ok) {
        const providerMessage = data?.error?.message || `HTTP ${response.status}`;
        console.error('Gemini request failed:', response.status, providerMessage);
        throw new Error(`Gemini could not refine the prompt (${response.status}): ${providerMessage}`);
    }

    const improvedPrompt = data.candidates?.[0]?.content?.parts?.[0]?.text?.trim();
    if (!improvedPrompt) {
        throw new Error('Gemini returned an empty prompt.');
    }

    return improvedPrompt;
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
