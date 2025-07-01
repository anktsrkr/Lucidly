window.speechRecognizer = {
    recognition: null,
    speechToText: '',
    finalSpeechToText: '',

    startRecognition: function (dotNetObject) {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;



        if (!SpeechRecognition) {
            alert("Speech recognition not supported in this browser."); //for Safari
            return;
        }
        const recognition = new SpeechRecognition();
        recognition.lang = 'en-US';
        recognition.interimResults = true;
        recognition.continuous = true;
        recognition.maxAlternatives = 1;
        recognition.onresult = function (event) {
            for (let i = event.resultIndex; i < event.results.length; ++i) {

                if (!event.results[i].isFinal) {
                    window.speechRecognizer.speechToText += event.results[i][0].transcript;
                }

                if (event.results[i].isFinal) {
                    window.speechRecognizer.finalSpeechToText += event.results[i][0].transcript;

                }
            }
            dotNetObject.invokeMethodAsync('OnSpeechRecognized', window.speechRecognizer.speechToText);


        };
        recognition.onerror = function (event) {
            console.error("Speech recognition error:", event.error);
        };
        recognition.start();
        window.speechRecognizer.recognition = recognition;
    },
    stopRecognition: function (dotNetObject) {
        dotNetObject.invokeMethodAsync('OnSpeechRecognized', window.speechRecognizer.finalSpeechToText);
        if (window.speechRecognizer.recognition) {
            window.speechRecognizer.recognition.stop();
        }
    },
    resetRecognition: function () {
        window.speechRecognizer.recognition = null;
        window.speechRecognizer.speechToText = '';
    }
};

window.TextToSpeech = {
    synth: null,
    speechToText: '',
    finalSpeechToText: '',

    startRecognition: function (dotNetObject,text) {
        window.TextToSpeech.synth = window.speechSynthesis;



        if (!window.TextToSpeech.synth) {
            alert("Speech recognition not supported in this browser."); //for Safari
            return;
        }
        const utterThis = new SpeechSynthesisUtterance(text);
        window.TextToSpeech.synth.speak(utterThis);
    },
    stopRecognition: function (dotNetObject) {
        dotNetObject.invokeMethodAsync('OnSpeechRecognized', window.speechRecognizer.finalSpeechToText);
        if (window.speechRecognizer.recognition) {
            window.speechRecognizer.recognition.stop();
        }
    },
    resetRecognition: function () {
        window.speechRecognizer.recognition = null;
        window.speechRecognizer.speechToText = '';
    }
};