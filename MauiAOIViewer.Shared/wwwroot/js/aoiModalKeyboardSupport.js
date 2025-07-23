window.aoiModalKeyboardSupport = {
    init: function (dotNetHelper) {
        document.addEventListener("keydown", function (e) {
            dotNetHelper.invokeMethodAsync("OnKeyDown", e.key);
        });
    },
    cleanup: function () {
        document.removeEventListener("keydown", () => { });
    }
};