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
        if (!$('#page_count').val() || isNaN($('#page_count').val())) {
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
        url: "/Distribution/WoerGetPartial?pageIndex=" + parseInt(pageIndex) +"&status=1"+ "&q=" + q,
        data: {},
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

function getCommissionList(obj, type) {
    var id = $(obj).parent("td").data("woerid");
    $.post("/Distribution/GetCommissionList", { uid: id, type: type }, function (res) {
        if (res.data) {
            var d = res.data;
            var h = '';
            var t = $("#listTpl").html();
            var str = type == 0 ? "+" : "-";
            for (var i = 0; i < d.length; i++) {
                h += t.replace("{{getcommissiontime}}", d[i].getcommissiontime).replace("{{title}}", d[i].title).replace("{{commission}}",str + d[i].commission).replace("{{finalcommission}}", d[i].finalcommission);
            }
            h = h == "" ? "暂无数据" : h;
            $("#ul_log").html(h);
            var title = type == 1 ? "结算记录" : "佣金明细";
            $(".rela span").text(title);
            $("#span_logTitle").text(title);
            $("#BDialog,.masklayer").css("display", "block");
        }
    });
}
function onsettle(obj) {
    var tar = $(obj).parent("td");
    var name = tar.data("realname");
    if (name == '') name = tar.data("woername");
    var id = tar.data("woerid");
    var commission = tar.data("commission");
    $("#hidden_commission").val(commission);
    $("#hidden_appid").val(id);//保存当前选中的uid
    $("#span_merName").text(name);
    $("#ADialog,.masklayer").css("display", "block");
}
function settle() {
    var id = $("#hidden_appid").val();
    if (!id) return;
    var amount = $("#czinput").val();
    if (!amount || isNaN(amount)) {
        alert("请输入结算金额");
        return;
    }
    var commission = $("#hidden_commission").val();
    if (parseFloat(amount*100) > parseFloat(commission)) {
        alert("结算金额不能大于佣金余额！");
        return;
    }
    $.post("/Distribution/Settle", { uid: id, amount: amount * 100 }, function (res) {
        if (res.status == "success") {
            alert("结算成功");
            $("#ADialog,.masklayer").css("display", "none");
        } else {
            alert("失败:" + res.message);
            $("#ADialog,.masklayer").css("display", "none");
        }
        refresh(1);
    });
}
//关闭
function closeClick() {
    $("#czinput").val("");
    $("#ADialog,.masklayer").css("display", "none");
    $("#BDialog,.masklayer").css("display", "none");
}
