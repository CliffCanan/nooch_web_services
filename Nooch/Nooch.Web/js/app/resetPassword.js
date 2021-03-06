﻿$(document).ready(function () {
    $(document).ajaxStart($.blockUI).ajaxStop($.unblockUI);

    if ($('#invalidUser').val() == "true")
    {
        swal({
            title: "Invalid User!",
            text: "Something went wrong - No user exists for this MemberID." +
                  "<small class='show' style='margin-top:12px'>Error Reference: <strong>\"Invalid User\"</strong></small>",
            type: "error",
            confirmButtonColor: "#3fabe1",
            confirmButtonText: "Ok",
            html: true
        });

        $('#pwFormShell').addClass('hidden');
    }

    $("#ResetPasswordButton").click(function () {

        var PWDTextBox = $('#PWDTextBox').val();
        var memberId = getParameterByName('memberId');
        var newUser = getParameterByName('newUser');

        if ($('#outerForm').parsley().validate() == true)
        {
            $.ajax({
                type: "POST",
                url: "ResetPasswordSubmit",
                data: "{PWDText: '" + PWDTextBox + "', memberId: '" + memberId + "',newUser:'" + newUser + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                async: "true",
                cache: "false",
                success: function (msg) {

                    if (msg == true)
                    {
                        $('#resetPasswordDiv').slideUp();
                        $('#depositHeader').slideUp();
                        $('#chk').fadeIn();
                        $('#iconCircleFA').html("<span class=\"fa-stack fa-lg\"><i class=\"fa fa-circle fa-stack-1x\" style=\"display: none;\"></i><i class=\"fa fa-stack-1x fa-inverse fa-thumbs-o-up\"></i></span>");

                        swal({
                            title: "Password Updated",
                            text: "Please try logging in with your new password.",
                            type: "success",
                            confirmButtonColor: "#3fabe1",
                            confirmButtonText: "Ok",
                            customClass: "largeText"
                        });
                    }
                    else
                        $('#resetPasswordDiv').css('display', 'block');
                },
                Error: function (x, e) {
                    console.log("RESET PASS ERROR. x:")
                    console.log(x);
                    console.log("e: " + e);
                }
            });
        }
        else
        {
            $('#iconCircleFA').one('webkitAnimationIteration oanimationiteration MSAnimationIteration animationiteration', function (e) {
                $('#iconCircleFA').removeClass('floating light-green-text').addClass('red-text shake');
            });
            $('#iconCircleFA').one('webkitAnimationEnd oanimationend msAnimationEnd animationend', function (e) {
                $('#iconCircleFA').removeClass('shake red-text').addClass('light-green-text floating');
            });
        }
    });


    $("#pinNumberVerificationButton").click(function () {

        var PINTextBox = $('#PINTextBox').val();
        var memberId = getParameterByName('memberId');

        if ($('#outerForm').parsley().validate() == true)
        {
            $.ajax({
                type: "POST",
                url: "pinNumberVerificationButton_Click",
                data: "{PINTextBox: '" + PINTextBox + "', memberId: '" + memberId + "'}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                async: "true",
                success: function (msg) {
                    console.log(msg.msg);

                    if (msg.isSuccess == true)
                    {
                        $('#resetPasswordDiv').css('display', 'block');

                        $('#pwFormShell').css('display', 'block');
                        $('#pwFormShell').addClass('col-xs-12 col-sm-offset-4 col-sm-4');
                    }
                    else
                    {
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
