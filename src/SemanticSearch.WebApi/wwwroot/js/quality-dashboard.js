window.qualityDashboard = (function () {
    const charts = new Map();

    function renderBreakdownChart(canvasId, slices) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !window.Chart) {
            return;
        }

        const labels = slices.map(slice => slice.category);
        const data = slices.map(slice => slice.lineCount);
        const backgroundColor = ['#0d5c63', '#c67b2c', '#2d6a4f'];

        if (charts.has(canvasId)) {
            charts.get(canvasId).destroy();
        }

        const chart = new window.Chart(canvas, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{ data, backgroundColor }]
            }
        });

        charts.set(canvasId, chart);
    }

    return {
        renderBreakdownChart
    };
})();
