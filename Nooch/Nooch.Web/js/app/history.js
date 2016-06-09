$(document).ready(function ()
{
    $(document).ready(function (e)
    {
        $("#TransactionMaster").trigger("click");

        $('#transMenuItem').addClass('active');

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

});


function showLocationModal(lati, longi, stte)
{
    var v = 'https://www.google.com/maps/embed/v1/place?q=' + lati + ',' + longi + '&center=' + lati + ',' + longi + '&key=AIzaSyDrUnX1gGpPL9fWmsWfhOxIDIy3t7YjcEY&zoom=13';
    $('#googleFrame').attr('src', v);

    $("#citystate").text(stte);

    $('#modal-transferLocation').modal();
    return false;
}
