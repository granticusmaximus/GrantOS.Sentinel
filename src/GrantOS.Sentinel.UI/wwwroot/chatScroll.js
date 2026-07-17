// Auto-scroll the chat transcript as its content changes. Driven by a MutationObserver
// rather than a per-Blazor-render call, so it reacts directly to DOM changes (including
// streamed token text updates) instead of depending on interop round-trip timing.
const observers = new WeakMap();

export function observeAndScroll(element) {
    if (!element || observers.has(element)) {
        return;
    }

    const observer = new MutationObserver(() => {
        element.scrollTop = element.scrollHeight;
    });
    observer.observe(element, { childList: true, subtree: true, characterData: true });
    observers.set(element, observer);

    element.scrollTop = element.scrollHeight;
}

export function unobserve(element) {
    const observer = observers.get(element);
    if (observer) {
        observer.disconnect();
        observers.delete(element);
    }
}
