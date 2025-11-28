
// --- FX Ticker using Yahoo Finance ---
// Logical pair name -> Yahoo Finance symbol
const fxPairs = [
    { code: "GBPUSD", yahoo: "GBPUSD=X", decimals: 5 },
    { code: "EURUSD", yahoo: "EURUSD=X", decimals: 5 },
    { code: "USDJPY", yahoo: "JPY=X",    decimals: 3 },  // Yahoo uses JPY=X for USD/JPY
    { code: "XAUUSD", yahoo: "XAUUSD=X", decimals: 2 },
    { code: "XAGUSD", yahoo: "XAGUSD=X", decimals: 3 }
];

async function fetchQuote(yahooSymbol) {
    const url = `https://query1.finance.yahoo.com/v7/finance/quote?symbols=${encodeURIComponent(yahooSymbol)}`;
    const res = await fetch(url);
    if (!res.ok) throw new Error("HTTP " + res.status);
    const data = await res.json();
    const q = data.quoteResponse && data.quoteResponse.result && data.quoteResponse.result[0];
    if (!q) throw new Error("No quote data");
    return q;
}

function updateFxDom(pairCode, price, changePercent) {
    const priceEl  = document.getElementById(pairCode + "-price");
    const changeEl = document.getElementById(pairCode + "-change");
    if (!priceEl || !changeEl) return;

    priceEl.textContent = price;

    // Clear existing classes
    changeEl.classList.remove("text-green", "text-red");
    const icon = changeEl.querySelector("i");

    if (changePercent > 0) {
        changeEl.classList.add("text-green");
        if (icon) icon.className = "bi bi-arrow-up text-green";
    } else if (changePercent < 0) {
        changeEl.classList.add("text-red");
        if (icon) icon.className = "bi bi-arrow-down text-red";
    } else {
        if (icon) icon.className = "bi bi-arrow-right text-white";
    }

    const sign = changePercent > 0 ? "+" : "";
    changeEl.appendChild(document.createTextNode(" " + sign + changePercent.toFixed(2) + "%"));
}

async function refreshFxTicker() {
    for (const pair of fxPairs) {
        try {
            const q = await fetchQuote(pair.yahoo);
            const price = q.regularMarketPrice != null
                ? q.regularMarketPrice.toFixed(pair.decimals)
                : "–";
            const chgPct = q.regularMarketChangePercent != null
                ? q.regularMarketChangePercent
                : 0;
            // Reset text node before appending
            const changeEl = document.getElementById(pair.code + "-change");
            if (changeEl) changeEl.textContent = "";
            updateFxDom(pair.code, price, chgPct);
        } catch (err) {
            console.error("FX ticker error for", pair.code, err);
        }
    }
}

// initial load
document.addEventListener("DOMContentLoaded", function () {
    refreshFxTicker();
    // refresh every 60 seconds
    setInterval(refreshFxTicker, 60000);
});
