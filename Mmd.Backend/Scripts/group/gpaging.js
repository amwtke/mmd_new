//////标签展示
function onPublished() {//已发布
    removeAllCurs();
    $('#label_published').addClass("cur");
    $('#s_status').html(0);
    refresh(1);
}
function onGroupOver() {//已结束
    removeAllCurs();
    $('#label_over').addClass("cur");
    $('#s_status').html(3);
    refresh(1);
}
function onToPublish() {//待发布
    removeAllCurs();
    $('#label_topublish').addClass("cur");
    $('#s_status').html(2);
    refresh(1);
}

function onRemoved() {//已删除
    removeAllCurs();
    $('#label_removed').addClass("cur");
    $('#s_status').html(1);
    refresh(1);
}

function removeAllCurs() {
    $('#label_published').removeClass("cur");
    $('#label_topublish').removeClass("cur");
    $('#label_removed').removeClass("cur");
    $('#label_over').removeClass("cur");
}
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
    var status = $('#s_status').html();
    if (q.length === 0) {
        $.ajax({
            type: "GET",
            url: "/Group/GroupGetPartial?pageIndex=" + parseInt(pageIndex) + "&q=" + q + "&status=" + status,
            data: {},
            datatype: "json",
            success: function (data) {
                $("#list_table").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    } else {
        $.ajax({
            type: "GET",
            url: "/Group/GroupGetPartial?pageIndex=" + parseInt(pageIndex) + "&q=" + "&status=" + status,
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

/////////////////////////删除团
function onGDelClick(id) {
    var gid = $('#gid_' + id).text();
    var title = $('#title_' + id).text();
    var pno = $('#pno_' + id).text();

    if (!confirm("是否要下架?\n团名:" + title + " \n商品编号:" + pno)) {
        return false;
    } else {
        delGroup(gid);
        return true;

    }
}


///上线团
function onGOnline(id) {
    var gid = $('#gid_' + id).text();
    var title = $('#title_' + id).text();
    var pno = $('#pno_' + id).text();

    if (!confirm("是否要上线?\n团名:" + title + " \n商品编号:" + pno)) {
        return false;
    } else {
        onlineGroup(gid);
        return true;

    }
}

///还原团
function onGResumeClick(id) {
    var gid = $('#gid_' + id).text();
    var title = $('#title_' + id).text();
    var pno = $('#pno_' + id).text();

    if (!confirm("是否要还原?\n团名:" + title + " \n商品编号:" + pno)) {
        return false;
    } else {
        onResume(gid);
        return true;

    }
}

////
function delGroup(gid) {
    var q = $('#s_text').val();
    $.ajax({
        type: "POST",
        url: "/Group/DelGroup",
        data: {gid:gid,q:q},
        datatype: "json",
        success: function (data) {
            alert("删除成功！");
            //刷新到第一页
            //$("#list_table").html(data);
            refresh(1);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
//预览
function onPreview(id) {
    var gid = $('#gid_' + id).text();
    var appid = $("#hidden_appid").val();
    $("#ADialog,.masklayer").css("display", "block");
    $("#woer_qrcode").empty();

    $.ajax({
        type: "GET",
        url: "/Group/GetPreviewUrl?appid=" + appid + "&gid=" + gid,
        data: {},
        datatype: "json",
        success: function (data) {
            $('#woer_qrcode').qrcode(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function onSendMsg(obj, gid) {
    if (confirm("每天只能推送一次活动，确定要推送该活动吗？")) {
        $(".sendmsgclass").remove();
        $.ajax({
            type: "post",
            url: "/Group/SendGroupNews?gid=" + gid,
            data: {},
            success: function (res) {
                if (res.status == "Success") {
                    alert("推送成功");
                } else if (res.status == "LimitTimesOut") {
                    alert("今日已推送过活动");
                } else {
                    alert(res.status);
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}

function sendImg() {
    var gid = $("#hidgid").val();
    $.ajax({
        type: "post",
        url: "/Group/SendGroupImg?gid=" + gid,
        data: {},
        success: function (res) {
            if (res.status == "Success") {
                alert("发送成功");
            } 
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function oncloseADialog() {
    $("#ADialog,.masklayer").css("display", "none");
}
//// 清空库存售罄
function onInventoryClear(gid) {

    var title = $('#title_' + gid).text();
    var pno = $('#pno_' + gid).text();
    var guidGid = $('#gid_' + gid).text();

    var q = $('#s_text').val();
    if (!confirm("是否要下线?\n团名:" + title + " \n商品编号:" + pno)) {
        return false;
    } else {
        $.ajax({
            type: "GET",
            url: "/Group/InventoryClear?gid=" + guidGid + "&q=" + q,
            data: {},
            datatype: "json",
            success: function (data) {
                alert("成功将团改成售罄状态！");
                //刷新到第一页
                $("#list_table").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}

function onlineGroup(gid) {
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/Group/Online?gid=" + gid + "&q=" + q,
        data: {},
        datatype: "json",
        success: function (data) {
            alert("上线成功！");

            //刷新到第一页
            $("#list_table").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}

function onResume(gid) {
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/Group/Resume?gid=" + gid + "&q=" + q,
        data: {},
        datatype: "json",
        success: function (data) {
            alert("还原成功！");

            //刷新到第一页
            $("#list_table").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}

/////////搜索
function onSearch() {
    refresh(1);
}

//显示推广链接
function onTuiguang(id) {
    var title = $('#title_' + id).text();
    var url = $('#url_' + id).text();
    $('#tan_gname').val(title);
    $('#tan_url').text(url);
    var gid = $("#gid_" + id).text();
    $("#hidgid").val(gid);
    //$.get('/Group/GetImgUrl?gid=' + gid, function (res) {
    //    $("#tuiguangimage").attr("src", res);
    //});
    $(".alertForm,.maskLayer").css("display", "block");
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer").css("display", "none"); $("#tuiguangimage").attr("src", ""); });
}
//增加库存弹框
function onAddKuCunAlert(gid) {
    var guidGid = $('#gid_' + gid).text();
    var title = $('#title_' + gid).text();
    var person_quo = $("#person_quota_" + gid).text();
    $("#span_groupName").text(title);
    $("#hidden_gid").val(guidGid);
    $("#hidden_person_quota").val(person_quo);
    $("#BDialog,.masklayer").css("display", "block");
}
//关闭增加库存弹框
function closeClick() {
    $("#BDialog,.masklayer").css("display", "none");
}
//增加库存
function onAddProduct_quota() {
    if (addquotaValita()) {
        var quota = $("#quotainput").val();
        var guidGid = $("#hidden_gid").val();
        var person_quota = $("#hidden_person_quota").val();
        //if (quota % person_quota != 0) {
        //    alert('库存必须是成团人数的整数倍!');
        //    return;
        //}
        var q = $('#s_text').val();
        $.ajax({
            type: "GET",
            url: "/Group/Addproduct_quota?gid=" + guidGid + "&product_quota=" + quota + "&q=" + q,
            data: {},
            datatype: "json",
            success: function (data) {
                alert("增加库存成功！");
                //刷新到第一页
                $("#list_table").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}
function addquotaValita() {
    var quota = $("#quotainput").val();
    var patt = new RegExp(/^[1-9]\d*$/);
    if (quota.match(patt)) {
        $("#txmessage").css('display', 'none');
        return true;
    } else {
        $('#txmessage').removeAttr("style");
    }
    return false;
}