(function () {
    const viewers = new Map();

    async function loadPdfJs() {
        try {
            const pdfjs = await import('/lib/pdfjs/pdf.min.mjs');
            pdfjs.GlobalWorkerOptions.workerSrc = '/lib/pdfjs/pdf.worker.min.mjs';
            return pdfjs;
        } catch (error) {
            console.error('Failed to load PDF.js assets.', error);
            throw new Error('The PDF viewer assets are unavailable. Refresh the app and verify the PDF.js files are deployed.');
        }
    }

    async function inspectDocument(pdfUrl, fallbackTitle) {
        const pdfjs = await loadPdfJs();
        const documentTask = pdfjs.getDocument(pdfUrl);

        try {
            const pdfDocument = await documentTask.promise;
            const pageCount = Math.max(1, pdfDocument.numPages || 1);
            const tocData = await extractTocData(pdfDocument, pageCount);
            const previewText = await extractPreviewText(pdfDocument, pageCount);
            const chapters =
                await getOutlineChapters(pdfDocument, pageCount) ||
                tocData.chapters ||
                [{ title: `Complete ${fallbackTitle || 'book'}`, startPage: 1, endPage: pageCount }];

            return {
                pageCount,
                chapters,
                tableOfContentsText: tocData.text,
                previewText
            };
        } finally {
            if (typeof documentTask.destroy === 'function') {
                await documentTask.destroy();
            }
        }
    }

    async function getOutlineChapters(pdfDocument, pageCount) {
        const outline = await pdfDocument.getOutline();
        if (!Array.isArray(outline) || outline.length === 0) {
            return null;
        }

        const entries = await flattenOutlineEntries(pdfDocument, outline, 0);
        if (entries.length === 0) {
            return null;
        }

        let candidates = entries.filter(entry => !entry.hasChildren && getStudyUnitPriority(entry.title) >= 60);
        if (candidates.length < 3) {
            candidates = entries.filter(entry => getStudyUnitPriority(entry.title) >= 80);
        }

        if (candidates.length < 3) {
            candidates = entries.filter(entry => !isSkippableMatter(entry.title) && entry.depth <= 1);
        }

        const chapters = buildChapterRanges(candidates, pageCount);
        return chapters.length > 1 ? chapters : null;
    }

    async function flattenOutlineEntries(pdfDocument, items, depth) {
        const entries = [];

        for (const item of items || []) {
            const title = item?.title?.replace(/\s+/g, ' ').trim();
            const pageNumber = await resolveDestinationPage(pdfDocument, item?.dest);
            const hasChildren = Array.isArray(item?.items) && item.items.length > 0;
            if (title && Number.isInteger(pageNumber)) {
                entries.push({ title, pageNumber, depth, hasChildren });
            }

            if (hasChildren) {
                entries.push(...await flattenOutlineEntries(pdfDocument, item.items, depth + 1));
            }
        }

        return entries;
    }

    async function resolveDestinationPage(pdfDocument, destination) {
        if (!destination) {
            return null;
        }

        let resolvedDestination = destination;
        if (typeof resolvedDestination === 'string') {
            resolvedDestination = await pdfDocument.getDestination(resolvedDestination);
        }

        if (!Array.isArray(resolvedDestination) || resolvedDestination.length === 0) {
            return null;
        }

        const pageReference = resolvedDestination[0];
        if (typeof pageReference === 'number') {
            return pageReference + 1;
        }

        return await pdfDocument.getPageIndex(pageReference) + 1;
    }

    async function extractTocData(pdfDocument, pageCount) {
        const candidates = [];
        const textSections = [];
        const pagesToInspect = Math.min(pageCount, 20);

        for (let pageNumber = 1; pageNumber <= pagesToInspect; pageNumber += 1) {
            const page = await pdfDocument.getPage(pageNumber);
            const textContent = await page.getTextContent();
            const lines = extractLines(textContent.items);
            const pageCandidates = [];

            for (const line of lines) {
                const candidate = parseTocLine(line, pageCount);
                if (candidate) {
                    candidates.push(candidate);
                    pageCandidates.push(candidate);
                }
            }

            const normalizedPageText = lines.join('\n').trim();
            const looksLikeContentsPage =
                /(^|\n)\s*(contents?|فهرس|المحتويات|محتويات)\s*($|\n)/i.test(normalizedPageText) ||
                pageCandidates.length >= 3;
            if (looksLikeContentsPage && normalizedPageText) {
                textSections.push(`Page ${pageNumber}\n${normalizedPageText}`);
            }
        }

        const chapters = buildChapterRanges(candidates, pageCount);
        return {
            chapters: chapters.length > 1 ? chapters : null,
            text: trimText(textSections.join('\n\n'), 7000)
        };
    }

    async function extractPreviewText(pdfDocument, pageCount) {
        const pagesToInspect = Math.min(pageCount, 6);
        const sections = [];

        for (let pageNumber = 1; pageNumber <= pagesToInspect; pageNumber += 1) {
            const page = await pdfDocument.getPage(pageNumber);
            const textContent = await page.getTextContent();
            const text = extractLines(textContent.items).join('\n').trim();
            if (!text) {
                continue;
            }

            sections.push(`Page ${pageNumber}\n${text}`);
        }

        return trimText(sections.join('\n\n'), 5000);
    }

    function extractLines(items) {
        const rows = new Map();

        for (const item of items || []) {
            const text = item?.str?.replace(/\s+/g, ' ').trim();
            if (!text) {
                continue;
            }

            const transform = item.transform || [];
            const y = Math.round(transform[5] || 0);
            const x = transform[4] || 0;
            const row = rows.get(y) || [];
            row.push({ x, text });
            rows.set(y, row);
        }

        return [...rows.entries()]
            .sort((left, right) => right[0] - left[0])
            .map(([, row]) =>
                row
                    .sort((left, right) => left.x - right.x)
                    .map(item => item.text)
                    .join(' ')
                    .replace(/\s+/g, ' ')
                    .trim())
            .filter(Boolean);
    }

    function parseTocLine(line, pageCount) {
        const normalized = line.replace(/\s+/g, ' ').trim();
        if (!normalized || normalized.length < 4 || normalized.length > 140) {
            return null;
        }

        if (/^contents?$/i.test(normalized)) {
            return null;
        }

        const match = normalized.match(/^(?<title>.+?)(?:\.{2,}|\s{2,}|\s+-\s+|\s)\s*(?<page>\d{1,4})$/);
        if (!match?.groups) {
            return null;
        }

        const startPage = Number.parseInt(match.groups.page, 10);
        if (!Number.isInteger(startPage) || startPage < 1 || startPage > pageCount) {
            return null;
        }

        const title = match.groups.title
            .replace(/\.+$/g, '')
            .replace(/\s+/g, ' ')
            .trim();

        if (!title) {
            return null;
        }

        if (getStudyUnitPriority(title) < 10) {
            return null;
        }

        return { title, startPage };
    }

    function buildChapterRanges(entries, pageCount) {
        const groupedByPage = [...entries]
            .map(entry => ({
                title: entry.title.replace(/\s+/g, ' ').trim(),
                startPage: Number.parseInt(entry.pageNumber ?? entry.startPage, 10)
            }))
            .filter(entry => entry.title && Number.isInteger(entry.startPage))
            .filter(entry => entry.startPage >= 1 && entry.startPage <= pageCount)
            .sort((left, right) => left.startPage - right.startPage || left.title.localeCompare(right.title))
            .reduce((groups, entry) => {
                const bucket = groups.get(entry.startPage) || [];
                bucket.push(entry);
                groups.set(entry.startPage, bucket);
                return groups;
            }, new Map());

        let normalized = [...groupedByPage.entries()]
            .map(([, group]) =>
                [...group]
                    .sort((left, right) =>
                        getStudyUnitPriority(right.title) - getStudyUnitPriority(left.title) ||
                        right.title.length - left.title.length ||
                        left.title.localeCompare(right.title))[0])
            .sort((left, right) => left.startPage - right.startPage || left.title.localeCompare(right.title));

        if (normalized.length === 0) {
            return [];
        }

        const specificStudyUnitCount = normalized.filter(entry => getStudyUnitPriority(entry.title) >= 60).length;
        if (specificStudyUnitCount >= 3) {
            const filtered = normalized.filter(entry => !isSkippableMatter(entry.title));
            if (filtered.length > 0) {
                normalized = filtered;
            }
        }

        if (normalized.filter(entry => getStudyUnitPriority(entry.title) >= 80).length >= 3) {
            const withoutContainers = normalized.filter(entry => !isContainerTitle(entry.title));
            if (withoutContainers.length > 0) {
                normalized = withoutContainers;
            }
        }

        const chapters = [];
        for (let index = 0; index < normalized.length; index += 1) {
            const current = normalized[index];
            const next = normalized[index + 1];
            const endPage = next
                ? Math.max(current.startPage, next.startPage - 1)
                : pageCount;

            chapters.push({
                title: current.title,
                startPage: current.startPage,
                endPage
            });
        }

        return chapters;
    }

    function isSkippableMatter(title) {
        return /^\s*(title page|half title|cover|copyright|dedication|contents?|table of contents|foreword|preface|acknowledg(e)?ments?|about the author|about the authors|index|glossary|bibliography|references?|المقدمة|مقدمة|تمهيد|الفهرس|فهرس المحتويات)\b/i.test(title);
    }

    function isContainerTitle(title) {
        return /^\s*(part|section|book|volume|مجلد|كتاب|جزء|قسم)\b/i.test(title);
    }

    function getStudyUnitPriority(title) {
        const normalized = title.trim();
        if (!normalized) {
            return 0;
        }

        if (isSkippableMatter(normalized)) {
            return 0;
        }

        if (/^\s*(chapter|appendix|lesson|module|unit|lecture|session|topic|act|فصل|الفصل|درس|الدرس|باب|الباب|موضوع|الموضوع)\b/i.test(normalized)) {
            return 100;
        }

        if (/^\s*\d+\s*[:.)-]?\s+\S/i.test(normalized)) {
            return 90;
        }

        if (isContainerTitle(normalized)) {
            return 35;
        }

        if (/^\s*(conclusion|summary|tests?|exercises?|solutions?|case study|epilogue|afterword|ملخص|خلاصة|خاتمة|تمرين|تمارين|أسئلة)\b/i.test(normalized)) {
            return 10;
        }

        const wordCount = normalized.split(/\s+/).filter(Boolean).length;
        if (wordCount >= 5) {
            return 75;
        }

        if (wordCount >= 3) {
            return 60;
        }

        if (wordCount === 2) {
            return 25;
        }

        return 15;
    }

    function trimText(value, maxLength) {
        if (!value) {
            return '';
        }

        return value.length <= maxLength
            ? value
            : `${value.slice(0, maxLength)}\n[truncated]`;
    }

    async function renderCurrentPage(containerId) {
        const state = viewers.get(containerId);
        if (!state) {
            return;
        }

        const page = await state.document.getPage(state.pageNumber);
        const viewport = page.getViewport({ scale: 1.25 });
        state.canvas.width = viewport.width;
        state.canvas.height = viewport.height;

        const context = state.canvas.getContext('2d');
        await page.render({ canvasContext: context, viewport }).promise;

        if (state.dotNetRef) {
            await state.dotNetRef.invokeMethodAsync('OnPageChanged', state.pageNumber);
        }
    }

    window.studyPdfViewer = {
        inspect: inspectDocument,

        async init(containerId, pdfUrl, initialPage, dotNetRef) {
            const pdfjs = await loadPdfJs();
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container '${containerId}' was not found.`);
            }

            const canvas = container.querySelector('canvas') ?? document.createElement('canvas');
            if (!canvas.parentElement) {
                container.appendChild(canvas);
            }

            const documentTask = pdfjs.getDocument(pdfUrl);
            const pdfDocument = await documentTask.promise;

            viewers.set(containerId, {
                document: pdfDocument,
                pageNumber: Math.max(1, Math.min(initialPage || 1, pdfDocument.numPages)),
                dotNetRef,
                canvas
            });

            await renderCurrentPage(containerId);
            return pdfDocument.numPages;
        },

        async renderPage(containerId, pageNumber) {
            const state = viewers.get(containerId);
            if (!state) {
                return 0;
            }

            state.pageNumber = Math.max(1, Math.min(pageNumber, state.document.numPages));
            await renderCurrentPage(containerId);
            return state.pageNumber;
        },

        getPageCount(containerId) {
            return viewers.get(containerId)?.document?.numPages ?? 0;
        },

        destroy(containerId) {
            const state = viewers.get(containerId);
            if (!state) {
                return;
            }

            viewers.delete(containerId);
        }
    };
})();
