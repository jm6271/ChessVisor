window.chessVisor = {
    getTheme: function () {
        try {
            return localStorage.getItem("chessvisor-theme") || "system";
        } catch {
            return "system";
        }
    },
    setTheme: function (theme) {
        try {
            localStorage.setItem("chessvisor-theme", theme);
        } catch {
        }
        const dark = theme === "dark" ||
            (theme === "system" && window.matchMedia("(prefers-color-scheme: dark)").matches);
        document.querySelector('meta[name="theme-color"]')
            ?.setAttribute("content", dark ? "#12100e" : "#f3ede3");
    },
    scrollToPly: function (position) {
        const container = document.querySelector(".move-list");
        if (!container) {
            return;
        }

        if (position === 0) {
            container.scrollTo({ top: 0, behavior: "smooth" });
            return;
        }

        const item = container.querySelector(`[data-ply="${position}"]`);
        if (!item) {
            return;
        }

        const containerRect = container.getBoundingClientRect();
        const itemRect = item.getBoundingClientRect();
        if (itemRect.top < containerRect.top || itemRect.bottom > containerRect.bottom) {
            const nextTop = container.scrollTop +
                itemRect.top -
                containerRect.top -
                (containerRect.height - itemRect.height) / 2;
            container.scrollTo({ top: nextTop, behavior: "smooth" });
        }
    },
    selectRange: function (element, offset, length) {
        if (!element) {
            return;
        }

        element.focus();
        element.setSelectionRange(offset, offset + length);
        const lineHeight = Number.parseFloat(getComputedStyle(element).lineHeight) || 22;
        const linesBefore = element.value.slice(0, offset).split("\n").length - 1;
        element.scrollTop = Math.max(0, (linesBefore - 2) * lineHeight);
    }
};
