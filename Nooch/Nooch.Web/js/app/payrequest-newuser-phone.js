function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

function hideform() {
    if ($("#hidfield").val() == "0") {
        console.log('method get called');


        $("#depositInstructions").html('');
        $("#depositInstructions").html('Looks like this transfer is no longer pending. Either you already responded by accepting or rejecting, or the sender cancelled it');

        $('#PayorInitialInfo').hide();
    }
}



$(document).ready(function () {

    var prodId = getParameterByName('TransactionId');
    console.log(prodId);
    hideform();
    ResetPage();
    setTimeout(bindPhone, 1000);

    $('#continueButtonclick').click(function () {
        continueClick();
    });

});


function bindPhone() {

console.log('came in this method '+$('#invitationSentto').val());
    $('#userPhone').val($('#invitationSentto').val());
}


function ResetPage() {

    var prodId = getParameterByName('TransactionId');


    // $('#PayorInitialInfo').show();
    $('#AddBankDiv').hide();

}



function continueClick() {
    console.log('got called');

    $.ajax({
        type: "POST",
        url: "deposit-newuserSynapseTest.aspx/RegisterUserWithSynp",
        data: "{ transId: '" + $('#hidfield').val() + "',userEmail: '" + $('#userEmail').val() + "',userPhone: '" + $('#userPhone').val() + "',userName: '" + $('#userName').val() + "',userPassword: '" + $('#userPassword').val() + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var RegisterUserWithSynpResult = msg.d;
            if (RegisterUserWithSynpResult.success == "true") {
                $('#PayorInitialInfo').hide();
                $('#AddBankDiv').show();

                //$("#frame").attr("src", "http://www.noochme.com/noochweb/MyAccounts/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated+"&RedUrl=http://www.noochme.com/transdone?transid=aasdsd");


               // $("#frame").attr("src", "http://localhost:4199/MyAccounts/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=http://localhost:4199/MyAccounts/payRequest-newuser-phone-complete-syn.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + $('#hidfield').val());


                $("#frame").attr("src", "http://54.201.43.89/noochweb/trans/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=http://54.201.43.89/noochweb/trans/payRequestComplete-syn.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + $('#hidfield').val());

                //$("#frame").attr("src", "https://www.noochme.com/noochweb/trans/Add-Bank.aspx?MemberId=" + RegisterUserWithSynpResult.memberIdGenerated + "&redUrl=https://www.noochme.com/noochweb/trans/payRequestComplete-syn.aspx?mem_id=" + RegisterUserWithSynpResult.memberIdGenerated + "," + $('#hidfield').val());



            } else {
                alert(RegisterUserWithSynpResult.reason);
            }

        },
        Error: function (x, e) {
            // On Error

            // Hide UIBlock (loading box))

        }
    });

}