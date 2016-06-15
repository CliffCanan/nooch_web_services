$(document).ready(function () {
    // Member.GetPageLoadData();

    $(function () {
        var iOS = false,
        p = navigator.platform;

        if (p === 'iPad' || p === 'iPhone' || p === 'iPod') {
            iOS = true;
            $('.iphoneLogin').removeClass('hidden');
        }
    });

    var initStatus = $('#init_status').val();

    if (initStatus != "pending") {

        var alertTitle = "";
        var alertBody = "";

        if (initStatus == "cancelled") {
            alertTitle = "Payment Already Cancelled";
            alertBody = "Looks like this payment was already cancelled, most likely by you. If this was cancelled by mistake, please contact <strong><a href='mailto:support@nooch.com' target='_blank'>Nooch Support</a></strong>.";
        }
        else if (initStatus == "rejected") {
            alertTitle = "Payment Already Rejected";
            alertBody = "Looks like this payment was already rejected. If you believe this was a mistake, please contact <strong><a href='mailto:support@nooch.com' target='_blank'>Nooch Support</a></strong>.";
        }
        else {
            alertTitle = "Payment No Longer Pending";
            alertBody = "Looks like this payment was already cancelled or rejected. If you believe this was a mistake, please contact <strong><a href='mailto:support@nooch.com' target='_blank'>Nooch Support</a></strong>.";
        }

        swal({
            title: alertTitle,
            text: alertBody,
            type: "warning",
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "Ok",
            html: true
        });
    }
});

function CancelReq() {
    var TransId = $('#TransId').val();
    var memberId = $('#memberId').val();
    var UserType = $('#UserType').val();
    $("#CancelBtn").text('Cancelling...');
    $('#CancelBtn').attr('disabled', 'disabled');

    var data = {};
    $.ajax({
        type: "POST",
        //url: URLs.CancelRequestFinal,
        url: $('#CancelRequestFinal').val() + "?TransId=" + TransId + "&memberId=" + memberId + "&UserType=" + UserType,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var Responce = msg;
            console.log("SUCCESS -> Transation Cancelled ... ");
            (Responce.succes == true)
            {
                $("#CancelBtn").text('Cancelled');
                //$('#CancelBtn').removeAttr("disabled");

                swal({
                    title: "Request Cancelled",
                    text: "<span class='show m-t-10'>" + Responce.resultMsg + "</span>",
                    type: "success",
                    showCancelButton: false,
                    confirmButtonColor: "#3fabe1",
                    confirmButtonText: "Ok",
                    html: true
                });
            }
        }
    });
}


// CLIFF (5/15/16): DON'T THINK THIS FILE IS USED AT ALL... EVERYTHING HAPPENS IN
//                  NoochController (code-behind methods).

/*var Member = function () {
    function GetPageLoadData() {

        var data = {};
        var TransactionId = getParameterByName('TransactionId');
        var MemberId = getParameterByName('MemberId');
        var UserType = getParameterByName('UserType');
        var url = "CancelRequestPageLoad"; // CLIFF (5/15/16): No method by this name exists in NoochController...

        data.TransactionId = TransactionId;
        data.MemberId = MemberId;
        data.UserType = UserType;

        $.post(url, data, function (result)
        {
            console.log(result);
            if (result.paymentInfo == "false")
            {
                $('#paymentInfo').css('display', 'none');
            }

            if (result.paymentInfo == "true") {
                
                $('#paymentInfo').css('display', 'block');
            }

            if (result.reslt1 == "false") {
                $('#reslt1').css('display', 'none');
            }

            if (result.reslt1 == "true") {

                $('#reslt1').css('display', 'block');
            }

            if (result.reslt != '')
            {
                $('#reslt').text(result.reslt);
                $('#reslt').css('display', 'block');
            }

            $('#senderImage').attr("src", result.senderImage);
            $('#nameLabel').text(result.nameLabel);
            $('#AmountLabel').text(result.AmountLabel);
        });
    }

    function getParameterByName(name, url) {
        if (!url) url = window.location.href;
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(url);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }

    return {
        GetPageLoadData: GetPageLoadData
    };
}();*/