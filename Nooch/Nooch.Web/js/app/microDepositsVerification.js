var FOR_RENTSCENE = $('#IsRs').val();
var isLrgScrn = false;

$(function ()
{
    $('.two-digits').keyup(function ()
    {
        if ($(this).val().indexOf('.') != -1) {
            if ($(this).val().split(".")[1].length > 2) {
                if (isNaN(parseFloat(this.value))) return;
                this.value = parseFloat(this.value).toFixed(2);
            }
        }
        return this; //for chaining
    });
});


$(document).ready(function ()
{
    if ($(window).width() > 1000) {
        isLrgScrn = true;
    }

    if (FOR_RENTSCENE == "true") {
        $('.landingHeaderLogo').attr('href', 'http://www.rentscene.com');
        $('.landingHeaderLogo img').attr('src', '../Assets/Images/rentscene.png');
        $('.landingHeaderLogo img').attr('alt', 'Rent Scene Logo');
        if (isLrgScrn)
            $('.landingHeaderLogo img').css('width', '211px');

        changeFavicon('../Assets/favicon2.ico');
    }


    $('#microVerForm').parsley();


    $(".two-digits").keydown(function (e)
    {
        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
            (e.keyCode == 65 && (e.ctrlKey === true || e.metaKey === true)) ||
            (e.keyCode >= 35 && e.keyCode <= 40)) {
            return;
        }

        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });
});



function SubmitInfo()
{
    console.log('SubmitInfo fired');

    if ($('#MicroDepositOne').val().length == 2) {

        $("#Submit").text('Submitting...');
        $('#Submit').attr('disabled', 'disabled');

        var dot = ".";
        var IsRs = $('#IsRs').val();
        var MemberId = $('#MemberId').val().trim();
        var MicroDepositOne = dot + $('#MicroDepositOne').val().trim();
        var MicroDepositTwo = dot + $('#MicroDepositTwo').val().trim();
        //var MicroDepositOne = $('#MicroDepositOne').val();
        //var MicroDepositTwo = $('#MicroDepositTwo').val();
        var NodeId1 = $('#NodeId1').val();
        var BankName = $('#BankName').val();


        console.log("SAVE MEMBER INFO -> {IsRs: " + IsRs +
                                       ", MemberId: " + MemberId +
                                       ", MicroDepositOne: " + MicroDepositOne +
                                        ", MicroDepositTwo: " + MicroDepositTwo +
                                         ", BankName: " + BankName +
                                       ", NodeId1 " + NodeId1 + "}");


        $.ajax({
            type: "POST",
            url: "MFALoginWithRoutingAndAccountNumber",
            //url: $('#SynapseV3MFABankVerifyWithMicroDepositsUrl').val(),
            data: "{  'Bank':'" + BankName +
                 "', 'MemberId':'" + MemberId +
                 "', 'MicroDepositOne':'" + MicroDepositOne +
                 "', 'MicroDepositTwo':'" + MicroDepositTwo +
                 "', 'NodeId1':'" + NodeId1 +
                 "', 'IsRs':'" + IsRs + "'}",

            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: "true",
            cache: "false",
            success: function (result)
            {
                console.log("SUCCESS -> Save Member Info result is... [next line]");
                console.log(result);
                resultReason = result.msg;

                $("#Submit").text('Submit');
                $('#Submit').removeAttr("disabled");

                if (result.Is_success == true) {
                    console.log("Success == true");


                    // THEN DISPLAY SUCCESS ALERT...
                    swal({
                        title: "Submitted Successfully",
                        text: "Thanks for submitting your information.",
                        type: "success",
                        showCancelButton: false,
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok",
                        closeOnConfirm: true,
                        customClass: "idVerSuccessAlert",
                    });
                }
                    //else if (result.mfaMessage.indexOf('Incorrect microdeposit amounts') != -1)
                    // swal({
                    //     title: "Unexpected Error",
                    //     text: result.mfaMessage,
                    //     type: "error",
                    //     confirmButtonColor: "#3fabe1",
                    //     confirmButtonText: "Ok",
                    //     html: true
                    // });

                else swal({
                    title: "Unexpected Error",
                    text: "Something went wrong - extremely sorry about this. We hate it when something breaks!",
                    type: "error",
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Ok",
                    html: true
                });

            }
        });
    }
}

$('body').on('focus', '.form-control', function ()
{
    $(this).closest('.fg-line').addClass('fg-toggled');
})

$('body').on('blur', '.form-control', function ()
{
    var fgrp = $(this).closest('.form-group');
    var ipgrp = $(this).closest('.input-group');

    var val = fgrp.find('.form-control').val();
    var val2 = ipgrp.find('.form-control').val();

    if (fgrp.hasClass('fg-float')) {
        if (val.length == 0) {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else if (ipgrp.hasClass('fg-float')) {
        if (val2.length == 0) {
            $(this).closest('.fg-line').removeClass('fg-toggled');
        }
    }
    else {
        $(this).closest('.fg-line').removeClass('fg-toggled');
    }
});

function changeFavicon(src)
{
    var link = document.createElement('link'),
     oldLink = document.getElementById('dynamic-favicon');
    link.id = 'dynamic-favicon';
    link.rel = 'shortcut icon';
    link.href = src;
    if (oldLink) {
        document.head.removeChild(oldLink);
    }
    document.head.appendChild(link);
}