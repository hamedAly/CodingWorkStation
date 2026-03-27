window.studyAudio = (() => {
    function setPlaybackRate(element, rate) {
        if (!element) {
            return;
        }

        element.playbackRate = rate;
    }

    return {
        setPlaybackRate
    };
})();