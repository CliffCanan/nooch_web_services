 

$(document).ready(function () {
 // Member.GetPageLoadData();

});

 
 



var Member = function () {
    function GetPageLoadData() {
        
        var data = {};
        var TransactionId = getParameterByName('TransactionId');
        var MemberId = getParameterByName('MemberId');
        var UserType = getParameterByName('UserType');
        var url = "CancelRequestPageLoad";
        data.TransactionId = TransactionId;
        data.MemberId = MemberId;
        data.UserType = UserType;
        $.post(url, data, function (result) {
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
}();