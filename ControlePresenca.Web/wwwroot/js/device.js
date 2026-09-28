export function getDeviceId() {
    const cookieName = "controle-presenca-device=";

    const cookies = document.cookie.split(";");

    for (const cookie of cookies) {
        const value = cookie.trim();

        if (value.startsWith(cookieName))
            return decodeURIComponent(value.substring(cookieName.length));
    }

    return "";
}