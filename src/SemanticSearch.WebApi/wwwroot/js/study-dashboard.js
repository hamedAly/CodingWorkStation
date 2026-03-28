window.studyDashboard = (function () {
    const charts = new Map();

    function renderBarChart(canvasId, labels, values, datasetLabel, color) {
        renderChart(canvasId, 'bar', labels, values, datasetLabel, color);
    }

    function renderLineChart(canvasId, labels, values, datasetLabel, color) {
        renderChart(canvasId, 'line', labels, values, datasetLabel, color);
    }

    function renderChart(canvasId, type, labels, values, datasetLabel, color) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !window.Chart) {
            return;
        }

        destroyChart(canvasId);

        const fillColor = type === 'line'
            ? color.replace('rgb', 'rgba').replace(')', ', 0.16)')
            : color;

        const chart = new window.Chart(canvas, {
            type,
            data: {
                labels,
                datasets: [{
                    label: datasetLabel,
                    data: values,
                    borderColor: color,
                    backgroundColor: fillColor,
                    borderRadius: type === 'bar' ? 10 : 0,
                    borderWidth: 2,
                    fill: type === 'line',
                    tension: type === 'line' ? 0.28 : 0,
                    pointRadius: type === 'line' ? 3 : 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(148, 163, 184, 0.18)'
                        },
                        ticks: {
                            color: '#456078'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#456078'
                        }
                    }
                }
            }
        });

        charts.set(canvasId, chart);
    }

    function destroyChart(canvasId) {
        if (!charts.has(canvasId)) {
            return;
        }

        charts.get(canvasId).destroy();
        charts.delete(canvasId);
    }

    return {
        renderBarChart,
        renderLineChart,
        destroyChart
    };
})();