var FOR_RENTSCENE = $('#IsRs').val();
var isLrgScrn = false;
var COMPANY = "Nooch";


$(document).ready(function () {
    if ($(window).width() > 1000) isLrgScrn = true;

    if (FOR_RENTSCENE == "true")
    {
        if (isLrgScrn) $('.landingHeaderLogo img').css('width', '170px');

        changeFavicon('../Assets/favicon2.ico');
        COMPANY = "Rent Scene";
    }

    if ($('#success').val() == "true")
        $('#microVerForm').parsley();

    if ($('#isAlrdyVer').val() == "true" && $('#hasPending').val() == "true")
    {
        // DISPLAY #pendingTrans IF USER HAS PENDING TRANSACTIONS
        $('#pendingTrans').removeClass('hidden').addClass('animated bounceIn');
    }

    $(".two-digits").keydown(function (e) {
        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
            (e.keyCode == 65 && (e.ctrlKey === true || e.metaKey === true)) ||
            (e.keyCode >= 35 && e.keyCode <= 40))
            return;

        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105))
            e.preventDefault();
    });
});


function SubmitInfo() {

    console.log('SubmitInfo fired');

    if ($('#MicroDepositOne').val().length == 2)
    {
        $("#Submit").text('Submitting...');
        $('#Submit').attr('disabled', 'disabled');

        var dot = ".";
        var IsRs = $('#IsRs').val();
        var MemberId = $('#MemberId').val().trim();
        var MicroDepositOne = dot + $('#MicroDepositOne').val().trim();
        var MicroDepositTwo = dot + $('#MicroDepositTwo').val().trim();
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
            data: "{ 'Bank':'" + BankName +
                 "', 'MemberId':'" + MemberId +
                 "', 'MicroDepositOne':'" + MicroDepositOne +
                 "', 'MicroDepositTwo':'" + MicroDepositTwo +
                 "', 'NodeId1':'" + NodeId1 + "'}",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: "true",
            cache: "false",
            success: function (result) {
                //console.log("SUCCESS -> MFALoginWithRoutingAndAccountNumber result is... [next line]");
                console.log(result);
                resultReason = result.msg;

                $("#Submit").text('Submit');
                $('#Submit').removeAttr("disabled");

                if (result.Is_success == true || result.Is_success == "True")
                {
                    console.log("Success!");

                    $('#idWizContainer').fadeOut(300);

                    setTimeout(function () {
                        $('.resultDiv').addClass('animated bounceIn').removeClass('hidden')
                    }, 350);

                    // THEN DISPLAY SUCCESS ALERT...
                    swal({
                        title: "Submitted Successfully",
                        text: "Thanks for verifying your bank account.",
                        type: "success",
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok"
                    });

                    // NOW DISPLAY #pendingTrans IF USER HAS PENDING TRANSACTIONS
                    if ($('#hasPending').val() == "true")
                        $('#pendingTrans').removeClass('hidden').addClass('animated bounceIn');
                }
                else if (result.errorMsg.indexOf("Incorrect microdeposit amounts") > -1)
                {
                    swal({
                        title: "Incorrect Amounts",
                        text: "Looks like those amounts were incorrect. Please try again!",
                        type: "error",
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok"
                    });
                }
                else if (result.errorMsg.indexOf("Microdeposits have not been sent to the bank") > -1)
                {
                    swal({
                        title: "Deposits Haven't Arrived yet!",
                        text: "The Microdeposits have not been sent to your bank account yet - please wait until you see two small deposits (< $1.00) on your bank statement within the next business day.",
                        type: "error",
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Ok"
                    });

                    $('#MicroDepositOne').attr('disabled', true).val('');
                    $('#MicroDepositTwo').attr('disabled', true).val('');
                }
                else
                {
                    swal({
                        title: "Unexpected Error",
                        text: "Something went wrong - extremely sorry about this. Please contact " + COMPANY + " support by clicking below.",
                        type: "error",
                        cancelButtonText: "OK",
                        confirmButtonColor: "#3fabe1",
                        confirmButtonText: "Contact Support",
                    }, function (isConfirm) {
                        if (isConfirm)
                        {
                            var supportEmail = COMPANY == "Rent Scene" ? "payments@rentscene.com" : "support@nooch.com";
                            window.open("mailto:" + supportEmail);
                        }
                    });
                }
            }
        });
    }
}


function SubmitPay(transId, recipId) {
    //$.blockUI();
    console.log('SubmitPay fired: ' + recipId);

    $('.btnPayRequest').attr('disabled', 'disabled');

    showLoadingHUD('Submitting Payment...');

    var MemberId = $('#MemberId').val().trim();

    $.ajax({
        type: "GET",
        url: "https://www.noochme.com/noochservice/api/NoochServices/GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit?TransactionId=" + transId + "&MemberId=" + MemberId + "&TransactionType=RequestToNewUser&recipMemId=" + recipId,
        dataType: "json",
        cache: false,
        crossDomain: true,
        success: function (data) {
            console.log(data);

            $('#pendingTrans .resultDiv').unblock();
            $('.btnPayRequest').removeAttr("disabled");

            if (data.synapseTransResult == 'Success')
            {
                swal({
                    title: "Paid",
                    text: "Request Paid Successfully.",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Done",
                });
            }
            else if (data.synapseTransResult == 'Request payor bank account details not found or syn user id not found')
            {
                swal({
                    title: "Error",
                    text: "User not found!",
                    type: "error",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1"
                });
            }
        },
        Error: function (data) {
            //$.UnblockUI();
            $('.btnPayRequest').removeAttr("disabled");
            swal({
                title: "Error",
                text: "User not found!",
                type: "error",
                showCancelButton: false,
                confirmButtonColor: "#3fabe1"
            });
        }
    });
}


function showLoadingHUD(msg) {
    $('#pendingTrans .resultDiv').block({
        message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">' + msg + '...</span>',
        css: {
            border: 'none',
            padding: '26px 8px 20px',
            backgroundColor: '#000',
            '-webkit-border-radius': '15px',
            '-moz-border-radius': '15px',
            'border-radius': '15px',
            opacity: '.75',
            width: '70%',
            left: '15%',
            top: '25px',
            color: '#fff'
        }
    });
}


$(function () {
    $('.two-digits').keyup(function () {
        if ($(this).val().indexOf('.') != -1)
        {
            if ($(this).val().split(".")[1].length > 2)
            {
                if (isNaN(parseFloat(this.value))) return;
                this.value = parseFloat(this.value).toFixed(2);
            }
        }
        return this; //for chaining
    });
});


$('body').on('focus', '.form-control', function () {
    $(this).closest('.fg-line').addClass('fg-toggled');
})

$('body').on('blur', '.form-control', function () {
    var fgrp = $(this).closest('.form-group');
    var ipgrp = $(this).closest('.input-group');

    var val = fgrp.find('.form-control').val();
    var val2 = ipgrp.find('.form-control').val();

    if (fgrp.hasClass('fg-float'))
    {
        if (val.length == 0)
            $(this).closest('.fg-line').removeClass('fg-toggled');
    }
    else if (ipgrp.hasClass('fg-float'))
    {
        if (val2.length == 0)
            $(this).closest('.fg-line').removeClass('fg-toggled');
    }
    else
        $(this).closest('.fg-line').removeClass('fg-toggled');
});

function changeFavicon(src) {
    var link = document.createElement('link'),
     oldLink = document.getElementById('dynamic-favicon');
    link.id = 'dynamic-favicon';
    link.rel = 'shortcut icon';
    link.href = src;
    if (oldLink) document.head.removeChild(oldLink);
    document.head.appendChild(link);
}