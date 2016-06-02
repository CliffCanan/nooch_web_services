$(function () {
    $('.two-digits').keyup(function () {
        if ($(this).val().indexOf('.') != -1) {
            if ($(this).val().split(".")[1].length > 2) {
                if (isNaN(parseFloat(this.value))) return;
                this.value = parseFloat(this.value).toFixed(2);
            }
        }
        return this; //for chaining
    });
});


$(document).ready(function () {
    $(".two-digits").keydown(function (e) {
        // Allow: backspace, delete, tab, escape, enter and .
        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
            // Allow: Ctrl+A, Command+A
            (e.keyCode == 65 && (e.ctrlKey === true || e.metaKey === true)) ||
            // Allow: home, end, left, right, down, up
            (e.keyCode >= 35 && e.keyCode <= 40)) {
            // let it happen, don't do anything
            return;
        }
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });
});