import { formatDate, formatMoney } from "@/utils";
import { describe, it, expect } from "vitest";

describe("formatMoney", () => {
    it("formats integer amounts correctly", () => {
        expect(formatMoney(1000)).toBe("1.000,00\u00A0€");
    });

    it("formats decimal amounts correctly", () => {
        expect(formatMoney(1234.56)).toBe("1.234,56\u00A0€");
    });

    it("formats negative amounts correctly", () => {
        expect(formatMoney(-50.5)).toBe("-50,50\u00A0€");
    });

    it("formats zero correctly", () => {
        expect(formatMoney(0)).toBe("0,00\u00A0€");
    });
});

describe("formatDate", () => {
    it("formats full date with time correctly", () => {
        const date = new Date(2023, 0, 15, 9, 5, 7, 45);
        // 15 Jan 2023, 09:05:07.045
        expect(formatDate(date)).toBe("15.01.2023 09:05:07:045");
    });

    it("pads single-digit day and month with leading zero", () => {
        const date = new Date(2023, 8, 5, 14, 30, 0, 9);
        // 5 Sep 2023, 14:30:00.009
        expect(formatDate(date)).toBe("05.09.2023 14:30:00:009");
    });

    it("handles end of year correctly", () => {
        const date = new Date(1999, 11, 31, 23, 59, 59, 999);
        expect(formatDate(date)).toBe("31.12.1999 23:59:59:999");
    });
});
