//////翻页按钮
function onLeftClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    if (currentPageNumber === 1)
        alert("已经是第1页了！");
    else {
        refresh(--currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}

function onRightClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    var totalPages = parseInt($("#totalPages").text());
    if (currentPageNumber === totalPages || totalPages === 0)
        alert("已经没有下一页了！");
    else {
        refresh(++currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}

function onJump() {
    try {
        //判断是否为数字
        if (isNaN($('#page_count').val())) {
            alert("请输入数字！");
        }
        else {
            var jumpToNumber = parseInt($('#page_count').val());
            var totalPages = parseInt($("#totalPages").text());
            if (jumpToNumber > totalPages)
                alert("超过最大页数！");
            else {
                refresh(jumpToNumber);
                $('#currentPage').text(jumpToNumber);
            }
        }
    } catch (e) {
        alert(e);
    }
}

function refresh(pageIndex) {
    var q = $('#s_text').val();

    $.ajax({
        type: "GET",
        url: "/MerchantCheck/GetList?pageIndex=" + parseInt(pageIndex) + "&q=" + q,
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

function onSearch() {
    refresh(1);
}

function onPass(id) {
    if (!confirm("是否要通过？")) {
        return;
    }
    var appid = $('#appid_' + id).text();
    var currentPageNumber = parseInt($('#currentPage').text());
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/MerchantCheck/Pass?pageIndex=" + currentPageNumber + "&q=" + q + "&appid=" + appid,
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

function onReject(id) {
    if (!confirm("是否不通过？")) {
        return;
    }

    var appid = $('#appid_' + id).text();
    var currentPageNumber = parseInt($('#currentPage').text());
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/MerchantCheck/Reject?pageIndex=" + currentPageNumber + "&q=" + q + "&appid=" + appid,
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

function onRecovery(id) {
    if (!confirm("是否确定将该商家设置为待审核状态？")) {
        return;
    }
    var appid = $('#appid_' + id).text();
    var currentPageNumber = parseInt($('#currentPage').text());
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/MerchantCheck/Recovery?pageIndex=" + currentPageNumber + "&q=" + q + "&appid=" + appid,
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


//充值关闭
function closeClick() {
    $("#ADialog,.masklayer").css("display", "none");
    $("#BDialog,.masklayer").css("display", "none");
}
//充值提交按钮事件
function CDDClick() {
    if (czValida()) {
        var currentPageNumber = parseInt($('#currentPage').text());
        var q = $('#s_text').val();
        var appid = $("#hidden_appid").val();
        var cddcount = $("#czinput").val();
        $.ajax({
            type: "GET",
            url: "/MerchantCheck/CDD?pageIndex=" + currentPageNumber + "&q=" + q + "&appid=" + appid + "&cddCount=" + cddcount,
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
}
//充值订单验证
function czValida() {
    var appid = $("#hidden_appid").val();
    if (appid == "") { alert('请刷新重试！'); }
    var czinput = $("#czinput").val();
    if (czinput.trim() == "" || czinput == "0") {
        $("#txmessage").removeAttr("style");
        return false;
    }
    var patt = new RegExp(/^[0-9]*$/);
    if (czinput.match(patt)) {
        $("#txmessage").css('display', 'none');
        return true;
    }
    else {
        $("#txmessage").removeAttr("style");
        return false;
    }
}

function bindczlog(mid, appname) {
    $.ajax({
        type: "GET",
        url: "/MerchantCheck/GetCzLog?mid=" + mid,
        data: {},
        datatype: "json",
        success: function (data) {
            var html = "";
            for (var i = 0; i < data.length; i++) {
                html += '<li><span>' + data[i].timestamp + '</span> 充值订单<span>' + data[i].buy_tc_shares + '</span></li>';
            }
            if (html != "") {
                $("#ul_log").html(html);
            }
            else {
                $("#ul_log").html("暂无数据");
            }
            $("#span_logTitle").text("【" + appname + "】" + "累计充值订单");
            $("#BDialog,.masklayer").css("display", "block");
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}