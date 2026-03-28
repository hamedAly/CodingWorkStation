window.studyFlashCard = (() => {
    function flipCard(cardElementId) {
        const card = document.getElementById(cardElementId);
        if (!card) {
            return;
        }

        card.classList.toggle('flipped');
    }

    function resetCard(cardElementId) {
        const card = document.getElementById(cardElementId);
        if (!card) {
            return;
        }

        card.classList.remove('flipped');
    }

    return {
        flipCard,
        resetCard
    };
})();