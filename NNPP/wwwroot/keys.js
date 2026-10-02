export function listen(dotnet) {
    const h = e => dotnet.invokeMethodAsync('OnKey', e.key, e.code, e.ctrlKey, e.shiftKey, e.altKey);
    document.addEventListener('keydown', h);
    return { dispose: () => document.removeEventListener('keydown', h) };
}