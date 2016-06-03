
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



function SubmitInfo() {
    console.log('SubmitInfo fired');

  
    var dot = ".";
    var IsRs = $('#IsRs').val();
    var MemberId = $('#MemberId').val().trim();
    var MicroDepositOne = dot+$('#MicroDepositOne').val();
    var MicroDepositTwo = dot+ $('#MicroDepositTwo').val();
    //var MicroDepositOne = $('#MicroDepositOne').val();
    //var MicroDepositTwo = $('#MicroDepositTwo').val();
    var bankId = $('#bankId').val();
    var BankName = $('#BankName').val();


    console.log("SAVE MEMBER INFO -> {IsRs: " + IsRs +
                                   ", MemberId: " + MemberId +
                                   ", MicroDepositOne: " + MicroDepositOne +
                                    ", MicroDepositTwo: " + MicroDepositTwo +
                                     ", BankName: " + BankName +
                                   ", bankId " + bankId + "}");


    $.ajax({
        type: "POST",
        url: "MFALoginWithRoutingAndAccountNumber",
        //url: $('#SynapseV3MFABankVerifyWithMicroDepositsUrl').val(),
        data: "{  'Bank':'" + BankName +
             "', 'MemberId':'" + MemberId +
             "', 'MicroDepositOne':'" + MicroDepositOne +
             "', 'MicroDepositTwo':'" + MicroDepositTwo +
             "', 'bankId':'" + bankId +
             "', 'IsRs':'" + IsRs + "'}",

        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (result) {
            console.log("SUCCESS -> Save Member Info result is... [next line]");
            console.log(result);
            resultReason = result.msg;


            if (result.Is_success == true) {
                console.log("Success == true");

                            
                // THEN DISPLAY SUCCESS ALERT...
                swal({
                    title: "Submitted Successfully",
                    text: "Thanks for submitting your information.",
                    //"<span>Next, link any checking account to complete this payment:</span>" +
                    //"<span class=\"spanlist\"><span>1. &nbsp;Select your bank</span><span>2. &nbsp;Login with your regular online banking credentials</span><span>3. &nbsp;Choose which account to use</span></span>",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Great!",
                    closeOnConfirm: true,
                    html: true,
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
    }
    )}

           