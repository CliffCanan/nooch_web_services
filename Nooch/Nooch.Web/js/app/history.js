﻿// unblock when ajax activity stops 
$(document).ajaxStop($.unblockUI);

$(document).ready(function (e)
{
    var isLrgScrn = $(window).width() > 1000 ? true : false;

    var user = getParameterByName("user");

    if (user == "rentscene") {
        $('.landingHeaderLogo').attr('href', 'http://www.rentscene.com');
        $('.landingHeaderLogo img').attr('src', '../Assets/Images/rentscene.png');
        $('.landingHeaderLogo img').attr('alt', 'Rent Scene Logo');
        if (isLrgScrn)
            $('.landingHeaderLogo img').css('width', '160px');

        changeFavicon('../Assets/favicon2.ico')
    }

    $('[data-toggle="tooltip"]').tooltip();

    $('#AllMembers')//.on( 'init.dt', function () {
        //console.log('Table initialisation complete. #1');
        //$('[data-toggle="tooltip"]').tooltip()
    //})
    .dataTable({
        responsive: true,
        "order": [3, "desc"],
        "initComplete": function (settings, json)
        {
            //console.log('Table initialisation complete. #2');
            $('[data-toggle="tooltip"]').tooltip()
        },
        "columnDefs": [
            { className: "actions", "targets": [-1] },
            { className: "p-5", "targets": [-2] },
            { "orderable": false, "targets": [0, -1] },
            { "width": "70px", "targets": -1 },
            //{"type": "date", "targets": 3},
        ],
        "language": {
            "info": "Showing _START_ to _END_ of _TOTAL_ Total Transactions"
        },
        "tableTools": {
            "sSwfPath": "../js/plugins/dataTables/swf/copy_csv_xls_pdf.swf"
        }
    });

    $(".btnCancel").click(function (e)
    {
        var btnClicked = $(this);
        var transId = btnClicked.attr('data-val');
        var UserType = btnClicked.attr('data-typ') == "" ? "new" : "existing";
        var memid = $('#memid').val();

        console.log("TransID: [" + transId + "], MemberID: [" + memid + "]");

        swal({
            title: "Are you sure?",
            text: "Do you want to cancel this transaction?",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "Yes - Cancel",
        }, function (isConfirm)
        {
            if (isConfirm) {
                var url = "cancelPayment";
                var data = {};
                data.TransId = transId;
                data.UserType = "EXISTING";
                data.memberId = memid;

                $.blockUI({
                    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Cancelling that payment...</span>',
                    css: {
                        border: 'none',
                        padding: '25px 8px 20px',
                        backgroundColor: '#000',
                        '-webkit-border-radius': '14px',
                        '-moz-border-radius': '14px',
                        'border-radius': '14px',
                        opacity: '.8',
                        width: '270px',
                        margin: '0 auto',
                        color: '#fff'
                    }
                });

                $.post(url, data, function (result)
                {
                    if (result.success == true) {
                        toastr.success('Transaction cancelled successfully ');
                        swal("Cancelled Successfully", result.resultMsg, "success");

                        $("#" + transId).html("<span class='text-danger'><strong>Cancelled</strong></span>");
                        $("#" + transId + " ~ .actions").html("");
                    }
                    else {
                        toastr.error(result.resultMsg, 'Error');
                    }
                });
            }
        });
    });
});

function showLocationModal(lati, longi, stte)
{
    var v = 'https://www.google.com/maps/embed/v1/place?q=' + lati + ',' + longi + '&center=' + lati + ',' + longi + '&key=AIzaSyDrUnX1gGpPL9fWmsWfhOxIDIy3t7YjcEY&zoom=13';
    $('#googleFrame').attr('src', v);

    $("#citystate").text(stte);

    $('#modal-transferLocation').modal();
    return false;
}

function getParameterByName(name)
{
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

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