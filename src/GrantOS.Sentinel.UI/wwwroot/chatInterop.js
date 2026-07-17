// Small DOM-level helpers for the Chat page that don't fit cleanly into Blazor's own
// declarative event/render model.

// Auto-scroll the chat transcript as its content changes. Driven by a MutationObserver
// rather than a per-Blazor-render call, so it reacts directly to DOM changes (including
// streamed token text updates) instead of depending on interop round-trip timing.
const scrollObservers = new WeakMap();

export function observeAndScroll(element) {
    if (!element || scrollObservers.has(element)) {
        return;
    }

    const observer = new MutationObserver(() => {
        element.scrollTop = element.scrollHeight;
    });
    observer.observe(element, { childList: true, subtree: true, characterData: true });
    scrollObservers.set(element, observer);

    element.scrollTop = element.scrollHeight;
}

export function unobserve(element) {
    const observer = scrollObservers.get(element);
    if (observer) {
        observer.disconnect();
        scrollObservers.delete(element);
    }
}

// Enter sends the message; Shift+Enter inserts a newline. Blazor's own
// @onkeydown:preventDefault is a static per-render flag, not something that can be decided
// per-keystroke based on which key was pressed - the browser would already have inserted the
// newline before a server round-trip could tell it not to. A native listener with
// preventDefault() called synchronously (only for a bare Enter) is the reliable way to do this.
const enterHandlers = new WeakMap();

export function bindEnterToSend(element, dotNetHelper) {
    if (!element || enterHandlers.has(element)) {
        return;
    }

    const handler = (event) => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            dotNetHelper.invokeMethodAsync("OnEnterPressed");
        }
    };
    element.addEventListener("keydown", handler);
    enterHandlers.set(element, handler);
}

export function unbindEnterToSend(element) {
    const handler = enterHandlers.get(element);
    if (handler) {
        element.removeEventListener("keydown", handler);
        enterHandlers.delete(element);
    }
}
