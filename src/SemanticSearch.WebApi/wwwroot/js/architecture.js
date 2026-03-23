window.architectureDashboard = (function () {
    const networks = new Map();
    const charts = new Map();

    // ── Dependency Graph (Vis.js Network) ───────────────────────────────────

    function renderDependencyGraph(containerId, nodes, edges, dotNetHelper) {
        const container = document.getElementById(containerId);
        if (!container || !window.vis) {
            return;
        }

        if (networks.has(containerId)) {
            networks.get(containerId).destroy();
        }

        const nodeDataSet = new vis.DataSet(nodes.map(n => ({
            id: n.nodeId,
            label: n.name,
            title: n.fullName,
            group: n.kind,
            color: n.kind === 'Class'
                ? { background: '#0d5c63', border: '#064a50', highlight: { background: '#1a8a94', border: '#0d5c63' } }
                : { background: '#c67b2c', border: '#a05e1a', highlight: { background: '#db8f35', border: '#c67b2c' } }
        })));

        const edgeDataSet = new vis.DataSet(edges.map(e => ({
            id: e.edgeId,
            from: e.sourceNodeId,
            to: e.targetNodeId,
            title: e.relationshipType,
            arrows: 'to',
            color: { color: '#6b7280', highlight: '#374151' }
        })));

        const options = {
            layout: { randomSeed: 42 },
            physics: {
                stabilization: { iterations: 100 },
                barnesHut: { gravitationalConstant: -8000, centralGravity: 0.3, springLength: 120 }
            },
            interaction: { hover: true, tooltipDelay: 200 },
            nodes: { shape: 'dot', size: 10, font: { size: 11 } },
            edges: { smooth: { type: 'dynamic' }, width: 1 }
        };

        const network = new vis.Network(container, { nodes: nodeDataSet, edges: edgeDataSet }, options);
        networks.set(containerId, network);

        // Click: highlight upstream/downstream connections
        network.on('click', function (params) {
            if (params.nodes.length === 0) {
                nodeDataSet.forEach(n => nodeDataSet.update({ id: n.id, opacity: 1.0 }));
                edgeDataSet.forEach(e => edgeDataSet.update({ id: e.id, color: { color: '#6b7280' }, width: 1 }));
                return;
            }

            const selectedId = params.nodes[0];
            const connectedEdges = network.getConnectedEdges(selectedId);
            const connectedNodes = new Set(network.getConnectedNodes(selectedId));
            connectedNodes.add(selectedId);

            nodeDataSet.forEach(n => {
                nodeDataSet.update({ id: n.id, opacity: connectedNodes.has(n.id) ? 1.0 : 0.2 });
            });

            edgeDataSet.forEach(e => {
                const isConnected = connectedEdges.includes(e.id);
                edgeDataSet.update({ id: e.id, color: { color: isConnected ? '#374151' : '#d1d5db' }, width: isConnected ? 2 : 1 });
            });

            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnNodeSelected', selectedId);
            }
        });

        // Double-click: reset highlight
        network.on('doubleClick', function () {
            nodeDataSet.forEach(n => nodeDataSet.update({ id: n.id, opacity: 1.0 }));
            edgeDataSet.forEach(e => edgeDataSet.update({ id: e.id, color: { color: '#6b7280' }, width: 1 }));
        });
    }

    function destroyDependencyGraph(containerId) {
        if (networks.has(containerId)) {
            networks.get(containerId).destroy();
            networks.delete(containerId);
        }
    }

    // ── Duplication Heatmap (Chart.js Treemap) ──────────────────────────────

    function renderHeatmap(canvasId, entries, dotNetHelper) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        if (charts.has(canvasId)) {
            destroyHeatmap(canvasId);
        }

        const host = canvas.parentElement;
        if (!host) {
            return;
        }

        const tooltip = document.createElement('div');
        tooltip.className = 'arch-heatmap-tooltip';
        tooltip.style.display = 'none';
        host.appendChild(tooltip);

        const state = createHeatmapState(canvas, host, tooltip, entries, dotNetHelper);
        state.render();

        host.addEventListener('mousemove', state.onMouseMove);
        host.addEventListener('mouseleave', state.onMouseLeave);
        host.addEventListener('click', state.onClick);
        window.addEventListener('resize', state.onResize);

        charts.set(canvasId, state);
    }

    function destroyHeatmap(canvasId) {
        if (charts.has(canvasId)) {
            const state = charts.get(canvasId);
            state.host.removeEventListener('mousemove', state.onMouseMove);
            state.host.removeEventListener('mouseleave', state.onMouseLeave);
            state.host.removeEventListener('click', state.onClick);
            window.removeEventListener('resize', state.onResize);
            if (state.tooltip && state.tooltip.parentElement) {
                state.tooltip.parentElement.removeChild(state.tooltip);
            }
            const ctx = state.canvas.getContext('2d');
            if (ctx) {
                ctx.clearRect(0, 0, state.canvas.width, state.canvas.height);
            }
            charts.delete(canvasId);
        }
    }

    function createHeatmapState(canvas, host, tooltip, entries, dotNetHelper) {
        const state = {
            canvas,
            host,
            tooltip,
            entries,
            dotNetHelper,
            rects: [],
            hoveredRect: null,
            onMouseMove: null,
            onMouseLeave: null,
            onClick: null,
            onResize: null,
            render: null
        };

        state.render = function () {
            const width = Math.max(host.clientWidth, 320);
            const height = Math.max(host.clientHeight, 320);
            const dpr = window.devicePixelRatio || 1;

            canvas.width = Math.floor(width * dpr);
            canvas.height = Math.floor(height * dpr);
            canvas.style.width = width + 'px';
            canvas.style.height = height + 'px';

            const ctx = canvas.getContext('2d');
            ctx.setTransform(1, 0, 0, 1, 0, 0);
            ctx.scale(dpr, dpr);
            ctx.clearRect(0, 0, width, height);

            state.rects = layoutTreemap(entries, 0, 0, width, height, true);
            drawTreemap(ctx, state.rects, state.hoveredRect?.entry?.relativeFilePath);
        };

        state.onMouseMove = function (event) {
            const point = getRelativePoint(host, event);
            const hovered = hitTest(state.rects, point.x, point.y);
            state.hoveredRect = hovered;
            state.render();

            if (!hovered) {
                tooltip.style.display = 'none';
                host.style.cursor = 'default';
                return;
            }

            host.style.cursor = 'pointer';
            tooltip.innerHTML = [
                `<strong>${escapeHtml(hovered.entry.fileName)}</strong>`,
                `<span>${escapeHtml(hovered.entry.relativeFilePath)}</span>`,
                `<span>Lines: ${hovered.entry.totalLines}</span>`,
                `<span>Structural: ${hovered.entry.structuralDuplicateCount}</span>`,
                `<span>Semantic: ${hovered.entry.semanticDuplicateCount}</span>`,
                `<span>Density: ${((hovered.entry.duplicationDensity || 0) * 100).toFixed(1)}%</span>`
            ].join('');
            tooltip.style.display = 'block';
            tooltip.style.left = Math.min(point.x + 16, host.clientWidth - 220) + 'px';
            tooltip.style.top = Math.min(point.y + 16, host.clientHeight - 140) + 'px';
        };

        state.onMouseLeave = function () {
            state.hoveredRect = null;
            tooltip.style.display = 'none';
            host.style.cursor = 'default';
            state.render();
        };

        state.onClick = function () {
            if (!state.hoveredRect || !dotNetHelper) {
                return;
            }

            dotNetHelper.invokeMethodAsync('OnFileClicked', state.hoveredRect.entry.relativeFilePath);
        };

        state.onResize = function () {
            state.render();
        };

        return state;
    }

    function layoutTreemap(entries, x, y, width, height, splitVertical) {
        if (!entries || entries.length === 0 || width <= 0 || height <= 0) {
            return [];
        }

        if (entries.length === 1) {
            return [{ x, y, width, height, entry: entries[0] }];
        }

        const total = entries.reduce((sum, entry) => sum + Math.max(entry.totalLines || 1, 1), 0);
        let running = 0;
        let splitIndex = 1;

        for (let i = 0; i < entries.length; i += 1) {
            running += Math.max(entries[i].totalLines || 1, 1);
            if (running >= total / 2) {
                splitIndex = i + 1;
                break;
            }
        }

        const first = entries.slice(0, splitIndex);
        const second = entries.slice(splitIndex);
        const firstTotal = first.reduce((sum, entry) => sum + Math.max(entry.totalLines || 1, 1), 0);
        const ratio = total === 0 ? 0.5 : firstTotal / total;

        if (splitVertical) {
            const firstWidth = Math.max(1, Math.round(width * ratio));
            return [
                ...layoutTreemap(first, x, y, firstWidth, height, !splitVertical),
                ...layoutTreemap(second, x + firstWidth, y, width - firstWidth, height, !splitVertical)
            ];
        }

        const firstHeight = Math.max(1, Math.round(height * ratio));
        return [
            ...layoutTreemap(first, x, y, width, firstHeight, !splitVertical),
            ...layoutTreemap(second, x, y + firstHeight, width, height - firstHeight, !splitVertical)
        ];
    }

    function drawTreemap(ctx, rects, hoveredPath) {
        rects.forEach(rect => {
            const entry = rect.entry;
            const isHovered = hoveredPath && hoveredPath === entry.relativeFilePath;
            const color = getHeatmapColor(entry.duplicationDensity || 0, isHovered);

            ctx.fillStyle = color;
            ctx.fillRect(rect.x + 1, rect.y + 1, Math.max(0, rect.width - 2), Math.max(0, rect.height - 2));

            ctx.strokeStyle = isHovered ? '#111827' : 'rgba(255,255,255,0.7)';
            ctx.lineWidth = isHovered ? 2 : 1;
            ctx.strokeRect(rect.x + 1, rect.y + 1, Math.max(0, rect.width - 2), Math.max(0, rect.height - 2));

            if (rect.width < 80 || rect.height < 34) {
                return;
            }

            ctx.save();
            ctx.beginPath();
            ctx.rect(rect.x + 4, rect.y + 4, Math.max(0, rect.width - 8), Math.max(0, rect.height - 8));
            ctx.clip();

            ctx.fillStyle = '#ffffff';
            ctx.font = '600 12px Segoe UI, sans-serif';
            ctx.fillText(entry.fileName, rect.x + 8, rect.y + 18);

            if (rect.height >= 50) {
                ctx.font = '11px Segoe UI, sans-serif';
                ctx.fillText(((entry.duplicationDensity || 0) * 100).toFixed(1) + '% density', rect.x + 8, rect.y + 34);
            }

            ctx.restore();
        });
    }

    function getHeatmapColor(density, isHovered) {
        const clamped = Math.max(0, Math.min(density, 1));
        const hue = Math.round(120 - clamped * 120);
        const lightness = isHovered ? 38 : 46;
        return `hsl(${hue}, 65%, ${lightness}%)`;
    }

    function getRelativePoint(host, event) {
        const rect = host.getBoundingClientRect();
        return {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };
    }

    function hitTest(rects, x, y) {
        for (let i = rects.length - 1; i >= 0; i -= 1) {
            const rect = rects[i];
            if (x >= rect.x && x <= rect.x + rect.width && y >= rect.y && y <= rect.y + rect.height) {
                return rect;
            }
        }

        return null;
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    // ── ER Diagram (Mermaid.js) ──────────────────────────────────────────────

    async function renderErDiagram(containerId, mermaidMarkup) {
        const container = document.getElementById(containerId);
        if (!container || !window.mermaid) {
            return;
        }

        try {
            window.mermaid.initialize({ startOnLoad: false, theme: 'default' });
            const uniqueId = 'er-svg-' + Date.now();
            const { svg } = await window.mermaid.render(uniqueId, mermaidMarkup);
            container.innerHTML = svg;

            // Enable wheel-zoom
            const svgEl = container.querySelector('svg');
            if (svgEl) {
                svgEl.style.maxWidth = '100%';
                let scale = 1;
                container.addEventListener('wheel', function (e) {
                    e.preventDefault();
                    scale += e.deltaY < 0 ? 0.1 : -0.1;
                    scale = Math.min(Math.max(scale, 0.3), 4);
                    svgEl.style.transform = `scale(${scale})`;
                    svgEl.style.transformOrigin = 'top left';
                }, { passive: false });
            }
        } catch (err) {
            container.innerHTML = '<p style="color:red">ER diagram render failed: ' + err.message + '</p>';
        }
    }

    return {
        renderDependencyGraph,
        destroyDependencyGraph,
        renderHeatmap,
        destroyHeatmap,
        renderErDiagram
    };
})();
