export async function toggleFullscreen(element) {
    if (!element)
        return;

    if (!document.fullscreenElement) {
        await element.requestFullscreen();
        return;
    }

    if (document.exitFullscreen)
        await document.exitFullscreen();
}