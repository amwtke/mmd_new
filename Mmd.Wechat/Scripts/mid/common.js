function midFW(parameters) {
    var mid = $('#mid').val();
    if (mid != null) {
        var days = $('#fwDays').val();
        if (days != null) {
            $.ajax({
                type: "GET",
                url: "/Mid/GetMidFwCount?mid="+mid+"&days="+days,
                data: {},
                datatype: "json",
                success: function (data) {
                    $("#fwLable").html(data);
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    alert(errorThrown);
                }
            });
        }
    }
}

function genTable() {
    var mid = $('#mid').val();
    if (mid.length === 0)
        return;
    $.ajax({
        type: "GET",
        url: "/Mid/GroupGetPartial?mid=" + mid,
        data: {},
        datatype: "json",
        success: function (data) {
            $("#list_table").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}