export function formatMoney(amount: number): string {
    return new Intl.NumberFormat("de-DE", {
        style: "currency",
        currency: "EUR",
    }).format(amount);
}

export function formatDate(date: Date): string {
    const pad = (num: number, size: number = 2): string =>
        String(num).padStart(size, "0");

    const day = pad(date.getDate());
    const month = pad(date.getMonth() + 1);
    const year = date.getFullYear();

    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    const seconds = pad(date.getSeconds());
    const milliseconds = pad(date.getMilliseconds(), 3);

    return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}:${milliseconds}`;
}
