

$(document).ready(function () {
    $("#ResetPasswordButton").click(function () {
        var PWDTextBox = $('#PWDTextBox').val();
        var memberId = getParameterByName('memberId');
        if ($('#form1').parsley().validate() == true) {
            $.ajax({
                type: "POST",
                url: "ResetPasswordButton_Click",
                data: "{PWDText: '" + PWDTextBox + "', memberId: '" + memberId + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                async: "true",
                cache: "false",
                success: function (msg) {

                    if (msg == true) {

                        $('#resetPasswordDiv').css('display', 'none');
                        $('#chk').css('display', 'block');
                        $('#r').html("<div id=\"iconCircleFA\" class=\"floating light-green-text\"><span class=\"fa-stack fa-lg\"><i class=\"fa fa-circle fa-stack-1x\" style=\"display: none;\"></i><i class=\"fa fa-stack-1x fa-inverse fa-thumbs-o-up\"></i></span></div>");
                        $('#messageDiv').css('display', 'block');
                        $('#m').css('display', 'none');
                        $('#pin').css('display', 'none');
                    }
                    else {

                        $('#resetPasswordDiv').css('display', 'block');
                        $('#messageDiv').css('display', 'none');
                    }
                },
                Error: function (x, e) {
                    console.log("RESET PASS ERROR. x:")
                    console.log(x);
                    console.log("e: " + e);

                }
            });
        }
    });




    $("#pinNumberVerificationButton").click(function () {
        var PINTextBox = $('#PINTextBox').val();
        var memberId = getParameterByName('memberId');
        var pin = $('#pin').css('display');
        if ($('#form1').parsley().validate() == true) {
            $.ajax({
                type: "POST",
                url: "pinNumberVerificationButton_Click",
                data: "{PINTextBox: '" + PINTextBox + "', memberId: '" + memberId + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                async: "true",

                success: function (msg) {
                    console.log(msg.msg);

                    if (msg.isSuccess == true) {

                        $('#pin').css('display', 'none');
                        $('#resetPasswordDiv').css('display', 'block');

                        $('#pwFormShell').css('display', 'block');
                        $('#pwFormShell').addClass('col-xs-12 col-sm-offset-4 col-sm-4');

                    }
                    else {

                        $('#messageLabel').css('display', 'block');
                        $('#messageLabel').text(msg.msg);
                    }
                },
                Error: function (x, e) {
                    alert(error);
                    console.log("RESET PASS ERROR. x:")
                    console.log(x);
                    console.log("e: " + e);

                }
            });
        }
    });

    function getParameterByName(name) {
       
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    }
});




