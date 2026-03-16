(function (global) {
    function parseColor(value, fallback) {
        return value || fallback;
    }

    function clear(canvas, ctx) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    }

    function drawSegment(ctx, centerX, centerY, radius, innerRadius, startAngle, endAngle, fillStyle) {
        ctx.beginPath();
        ctx.moveTo(centerX, centerY);
        ctx.arc(centerX, centerY, radius, startAngle, endAngle);
        ctx.closePath();
        ctx.fillStyle = fillStyle;
        ctx.fill();

        ctx.save();
        ctx.globalCompositeOperation = 'destination-out';
        ctx.beginPath();
        ctx.arc(centerX, centerY, innerRadius, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }

    function Chart(canvas, config) {
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
        this.config = config || { data: { datasets: [{ data: [], backgroundColor: [] }] } };
        this.render();
    }

    Chart.prototype.destroy = function () {
        if (this.ctx) {
            clear(this.canvas, this.ctx);
        }
    };

    Chart.prototype.update = function () {
        this.render();
    };

    Chart.prototype.render = function () {
        var dataset = (this.config.data && this.config.data.datasets && this.config.data.datasets[0]) || { data: [], backgroundColor: [] };
        var data = dataset.data || [];
        var colors = dataset.backgroundColor || [];
        var total = data.reduce(function (sum, current) { return sum + current; }, 0);
        var canvas = this.canvas;
        var ctx = this.ctx;
        var size = Math.min(canvas.width || 240, canvas.height || 240) || 240;
        canvas.width = size;
        canvas.height = size;
        clear(canvas, ctx);

        if (!total) {
            ctx.beginPath();
            ctx.arc(size / 2, size / 2, size / 2 - 8, 0, Math.PI * 2);
            ctx.strokeStyle = '#d7d0c2';
            ctx.lineWidth = 24;
            ctx.stroke();
            return;
        }

        var start = -Math.PI / 2;
        for (var i = 0; i < data.length; i += 1) {
            var value = data[i];
            var angle = (value / total) * Math.PI * 2;
            drawSegment(ctx, size / 2, size / 2, size / 2 - 8, size / 2 - 36, start, start + angle, parseColor(colors[i], '#0d5c63'));
            start += angle;
        }
    };

    global.Chart = global.Chart || Chart;
})(window);
