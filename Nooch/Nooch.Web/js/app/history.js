// unblock when ajax activity stops 

var COMPANY = "Nooch";

$(document).ajaxStop($.unblockUI);

$(document).ready(function (e)
{
    var isLrgScrn = $(window).width() > 1000 ? true : false;

    var user = getParameterByName("user");

    if (user == "rentscene") {
        if (isLrgScrn)
            $('.landingHeaderLogo img').css('width', '160px');

        changeFavicon('../Assets/favicon2.ico')
    }
    else if (user == "habitat")
    {
        COMPANY = "Habitat";
        changeFavicon('../Assets/favicon-habitat.png')
    }

    $('[data-toggle="tooltip"]').tooltip();

    var table = $('#AllMembers').DataTable({
        responsive: true,
        "order": [4, "desc"],
        "initComplete": function (settings, json)
        {
            //console.log('Table initialisation complete. #2');
            $('[data-toggle="tooltip"]').tooltip()
        },
        "columnDefs": [
            { className: "actions", "targets": [-1] },
            { className: "p-5", "targets": [-2] },
            { className: "text-center", "targets": [0, 3, 4]},
            { "orderable": false, "targets": [0, 1, -1] },
            { "width": "70px", "targets": -1 },
            //{"type": "date", "targets": 3},
        ],
        "language": {
            "info": "Showing _START_ to _END_ of _TOTAL_ Total Transactions"
        },
        "pageLength": 25,
        "tableTools": {
            "sSwfPath": "../js/plugins/dataTables/swf/copy_csv_xls_pdf.swf"
        }
    });

    table.on('order.dt search.dt', function ()
    {
        table.column(0, { search: 'applied', order: 'applied' }).nodes().each(function (cell, i)
        {
            cell.innerHTML = i + 1;
        });
    }).draw();


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
                    else
                        toastr.error(result.resultMsg, 'Error');
                });
            }
        });
    });

    $(".btnRemind").click(function (e) {
        var btnClicked = $(this);
        var transId = btnClicked.attr('data-val');
        var UserType = btnClicked.attr('data-typ') == "" ? "new" : "existing";
        var memid = $('#memid').val();

        console.log("TransID: [" + transId + "], MemberID: [" + memid + "]");

        swal({
            title: "Are you sure?",
            text: "Do you want to Send Reminder for this transaction?",
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "Yes - Send",
        }, function (isConfirm) {
            if (isConfirm) {
                var url = "paymentReminder";
                var data = {};
                data.TransId = transId;
                data.UserType = "EXISTING";
                data.memberId = memid;

                $.blockUI({
                    message: '<span><i class="fa fa-refresh fa-spin fa-loading"></i></span><br/><span class="loadingMsg">Sending reminder...</span>',
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

                $.post(url, data, function (result) {
                    if (result.success == true)
                    {
                        toastr.success('Reminder sent successfully');
                        swal("Reminder Sent", result.resultMsg, "success");
                    }
                    else
                        toastr.error(result.resultMsg, 'Error');
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

function changeFavicon(src) {
    var link = document.createElement('link'),
     oldLink = document.getElementById('dynamic-favicon');
    link.id = 'dynamic-favicon';
    link.rel = 'shortcut icon';
    link.href = src;
    if (oldLink) document.head.removeChild(oldLink);
    document.head.appendChild(link);
}