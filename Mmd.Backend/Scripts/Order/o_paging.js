
//////标签展示
function onAll() {//所有订单
    removeAllCurs();
    $("#div_writeoffpoint").css("display", "none");
    $("#div_writeoffpointer").css("display", "none");
    $('#label_all').addClass("cur");
    $('#s_status').html(-1);
    $("#s_wtg").html(-1);
    refresh(1);
}

function onDTH() {//待提货
    removeAllCurs();
    $("#div_writeoffpoint").css("display", "block");
    $("#div_writeoffpointer").css("display", "none");
    $('#label_dth').addClass("cur");
    $('#s_status').html(5);
    $("#s_wtg").html(0);
    refresh(1);
}

function onYTH() {//已提货
    removeAllCurs();
    $("#div_writeoffpoint").css("display", "block");
    $("#div_writeoffpointer").css("display", "block");
    $('#label_yth').addClass("cur");
    $('#s_status').html(6);
    $("#s_wtg").html(0);
    refresh(1);
}
//物流订单
function onLogiOrder(obj, status) {
    $("#div_writeoffpoint").css("display", "none");
    $("#div_writeoffpointer").css("display", "none");
    $('#s_status').html(status);
    $("#s_wtg").html(1);
    $(obj).addClass("cur").siblings().removeClass("cur");
    refresh(1);
}

function removeAllCurs() {
    $("a.logiOrder").removeClass("cur");
    $('#label_all').removeClass("cur");
    $('#label_dth').removeClass("cur");
    $('#label_yth').removeClass("cur");
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
    var qdate = $("#reservation").val();//时间段
    var status = $('#s_status').html();
    var wtg = $('#s_wtg').html();
    var q = $('#s_text').val();
    var writeoffpoint = $("#select_writeoffpoint").val();
    var writeoffer = $('#select_writeoffpointer').val();
    if (status === "-1") {
        $.ajax({
            type: "GET",
            url: "/Order/OrderGetPartial?qdate=" + qdate + "&pageIndex=" + parseInt(pageIndex) + "&q=" + q + "&writeoffpoint=",
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
            url: "/Order/OrderGetPartial?qdate=" + qdate + "&pageIndex=" +
                parseInt(pageIndex) +
                "&q=" +
                q +
                "&status=" +
                status +
                "&wtg=" +
                wtg +
                "&writeoffpoint=" + writeoffpoint+
            "&writeofferid=" + writeoffer,
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

function onSearch() {
    refresh(1);
}
function distribution(obj) {
    var oid = $(obj).data("oid");
    if (oid) {
        $.ajax({
            type: "POST",
            url: "/Order/Distribution",
            data: { oid: oid },
            success: function (res) {
                if (res && res.status == "Success") {
                    $(obj).remove();
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                console.log(errorThrown);
            }
        });
    }
}
function shipment(obj) {
    var oid = $(obj).data("oid");
    if (oid) {
        var tar = $(obj).parents("tr");
        var com = tar.find(".postCompany").val();
        var num = tar.find(".postNumber").val();
        if (!com) {
            alert("请选择快递公司");
            return;
        }
        if (!num) {
            alert("请输入快递单号");
            return;
        }
        $.ajax({
            type: "POST",
            url: "/Order/Shipment",
            data: { oid: oid, post_company: com, post_number: num },
            success: function (res) {
                if (res && res.status == "Success") {
                    $(obj).remove();
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                console.log(errorThrown);
            }
        });
    }
}
function shipmentview(obj) {
    var com = $(obj).data("com");
    var num = $(obj).data("num");
    var url = 'https://m.kuaidi100.com/index_all.html?type=' + com + '&postid=' + num;
    $("#logisticsInfo").append('<iframe src="'+url+'" style="width:100%;height:100%;"></iframe>')
    $(".alertForm").show();
    $(".i-close").click(function () { $(".alertForm").hide(); $("#logisticsInfo").html(""); });
}
function onExport() {
    var qdate = $("#reservation").val();//时间段
    var status = $('#s_status').html();
    var q = $('#s_text').val();
    var wtg = $("#s_wtg").html();
    var writeoffpoint = $("#select_writeoffpoint").val();
    var writeoffer = $('#select_writeoffpointer').val();
    if (status == "")
        status = "-1";
    if (status !== "") {
        window.open("/Order/ExportCsv?writeoffpoint=" + writeoffpoint + "&writeofferid=" + writeoffer + "&qdate=" + qdate + "&status=" + status + "&q=" + q + "&waytoget=" + wtg);
    }
}
function bindSelect() {
    $.ajax({
        type: "GET",
        url: "/Order/GetWriteOffPoint",
        datatype: "json",
        success: function (data) {
            var strhtml = '<option value="">全部</option>';
            if (data != "" && data.length > 0) {
                for (var i = 0; i < data.length; i++) {
                    strhtml += '<option value="' + data[i].woid + '">' + data[i].name + '</option>';
                }
                $("#select_writeoffpoint").html(strhtml);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
//核销员下拉框
function bindWriteOffPointer() {
    var woid = $("#select_writeoffpoint").val();
    if (woid) {
        $.ajax({
            type: "POST",
            url: "/Order/GetWriteOffPointer",
            data: { woid: woid },
            success: function (res) {
                var strhtml = '<option value="">全部</option>';
                if (res != "" && res.data) {
                    var d = res.data;
                    for (var i = 0; i < d.length; i++) {
                        strhtml += '<option value="' + d[i].uid + '">' + d[i].realname + '</option>';
                    }
                    $("#select_writeoffpointer").html(strhtml);
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                console.log(errorThrown);
            }
        });
    }
}
//核销弹框
function writeofforder(appid, oid) {
    $("#ADialog,.masklayer").css("display", "block");
    $("#woer_qrcode").empty();
    $.ajax({
        type: "GET",
        url: "/Order/GetWriteOffUrl?appid=" + appid + "&oid=" + oid,
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
function oncloseADialog() {
    $("#ADialog,.masklayer").css("display", "none");
}
function OrderRefund(name, appid, o_no) {
    if (appid == "" || o_no == "") {
        alert('参数错误！');
    }
    if (confirm("您确定要对此订单（购买人：" + name + "，订单号：" + o_no + "）执行退款操作吗？")) {
        $.ajax({
            type: "POST",
            url: "/Order/OrderRefund",
            data: { appid: appid, o_no: o_no },
            datatype: "json",
            success: function (data) {
                if (data.flag == true) {
                    alert(data.retMessage);
                    refresh(1);
                } else {
                    alert(data.retMessage);
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}