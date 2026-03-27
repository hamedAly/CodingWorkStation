window.studyTimer = (function () {
    let intervalId = null;
    let remainingSeconds = 0;
    let dotNetHelper = null;

    function notifyTick() {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnTimerTick', remainingSeconds);
        }
    }

    function tick() {
        remainingSeconds = Math.max(0, remainingSeconds - 1);
        notifyTick();

        if (remainingSeconds === 0) {
            pauseTimer();
            playCompletionTone();
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnTimerComplete');
            }
        }
    }

    function startTimer(totalSeconds, helper) {
        destroy();
        remainingSeconds = Math.max(0, totalSeconds || 0);
        dotNetHelper = helper || null;
        notifyTick();

        if (remainingSeconds > 0) {
            intervalId = window.setInterval(tick, 1000);
        }
    }

    function pauseTimer() {
        if (intervalId) {
            window.clearInterval(intervalId);
            intervalId = null;
        }

        return remainingSeconds;
    }

    function resumeTimer() {
        if (intervalId || remainingSeconds <= 0) {
            return remainingSeconds;
        }

        intervalId = window.setInterval(tick, 1000);
        notifyTick();
        return remainingSeconds;
    }

    function getRemaining() {
        return remainingSeconds;
    }

    function destroy() {
        if (intervalId) {
            window.clearInterval(intervalId);
            intervalId = null;
        }

        remainingSeconds = 0;
        dotNetHelper = null;
    }

    function playCompletionTone() {
        const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextCtor) {
            return;
        }

        const context = new AudioContextCtor();
        const oscillator = context.createOscillator();
        const gain = context.createGain();

        oscillator.type = 'sine';
        oscillator.frequency.setValueAtTime(880, context.currentTime);
        gain.gain.setValueAtTime(0.0001, context.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.18, context.currentTime + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.45);

        oscillator.connect(gain);
        gain.connect(context.destination);
        oscillator.start();
        oscillator.stop(context.currentTime + 0.45);
        oscillator.onended = function () {
            context.close();
        };
    }

    return {
        startTimer,
        pauseTimer,
        resumeTimer,
        getRemaining,
        destroy
    };
})();