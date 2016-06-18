$(document).ready(function () {
    // Member.GetPageLoadData();

    // CC (6/18/16): Commenting out since the App is temporarily down after V3 upgrade.
    //$(function () {
    //    var iOS = false,
    //    p = navigator.platform;

    //    if (p === 'iPad' || p === 'iPhone' || p === 'iPod') {
    //        iOS = true;
    //        $('.iphoneLogin').removeClass('hidden');
    //    }
    //});

    var initStatus = $('#init_status').val();

    if (initStatus != "pending")
    {
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

        $('#fromText').removeClass('hidden');
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
        url: $('#CancelRequestFinal').val() + "?TransId=" + TransId + "&memberId=" + memberId + "&UserType=" + UserType,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: "true",
        cache: "false",
        success: function (msg) {
            var response = msg;
            console.log("SUCCESS -> Transation Cancelled ... ");
            (response.succes == true)
            {
                $('#CancelBtn').removeAttr("disabled");
                $("#CancelBtn").addClass('animated bounceOut');

                $('#cancalledHdr').removeClass('hidden');

                swal({
                    title: "Request Cancelled",
                    text: "<span class='show m-t-10'>" + response.resultMsg + "</span>",
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
