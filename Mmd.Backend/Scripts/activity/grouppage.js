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

//预览
function onPreview(id) {
    var appid = $("#hidden_appid").val();
    $("#ADialog,.maskLayer").css("display", "block");
    $("#woer_qrcode").empty();
    var url = "http://" + $("#hidden_appid").val() + ".wx.mmpintuan.com/mmpt/app/#/app/toolbox/jttpreview/" + id + "&Robot862648097892454399be69600ba5678a";
    $('#woer_qrcode').qrcode(url);
}
//显示推广链接
function onTuiguang(obj,title) {
    var url = $(obj).data("prourl");
    $('#tan_gname').val(title);
    $('#tan_url').text(url);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer").css("display", "none"); });
}
function renderPic(obj,title) {
    var url = $(obj).data("prourl");
    //var url = "http://localhost:2895/Group/GoLadderDetail?_a=wxa2f78f4dfc3b8ab6&_i=d798a680-0262-43db-a5bb-5ec55191b001";
    $('#tan_gname').val(title);
    $("#ADialog,.maskLayer").css("display", "block");
    $("#woer_qrcode").empty();
    $('#woer_qrcode').qrcode(url);
    
    html2canvas($("#ADialog")).then(function (canvas) {
        //以下代码为下载此图片功能
        var triggerDownload = $("<a>").attr("href", canvas.toDataURL()).attr("download", "test.png").text("下载海报");
        $("#ADialog").append(triggerDownload);
        console.log(canvas.toDataURL());
    });
} 
function oncloseADialog() {
    $("#ADialog,#BDialog,.maskLayer").css("display", "none");
    $("#quotainput").val("");
}
//增加库存弹框
function onAddCountAlert(guidGid, title) {
    $("#span_groupName").text(title);
    $("#hidden_gid").val(guidGid);
    $("#BDialog,.maskLayer").css("display", "block");
}
//增加库存
function onAddProduct_quota() {
    if (addquotaValita()) {
        var quota = $("#quotainput").val();
        var guidGid = $("#hidden_gid").val();
        $.ajax({
            type: "POST",
            url: "/LadderGroup/Addproduct_quota",
            data: { gid: guidGid, product_quota: quota },
            success: function (data) {
                alert("增加库存成功！");
                oncloseADialog();
                //刷新到第一页
                refresh(1);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}
function addquotaValita() {
    var patt = new RegExp(/^[1-9]\d*$/);
    var quota = $("#quotainput").val();
    if (quota.match(patt)) {
        $("#txmessage").css('display', 'none');
        return true;
    } else {
        $('#txmessage').removeAttr("style");
    }
    return false;
}
function changeStatus(id,status) {
    if (status == 4) {
        if (!confirm("确定要下线当前活动吗？")) {
            return;
        }   
    }
    if (status == 1) {
        if (!confirm("确定要删除当前活动吗？")) {
            return;
        }
    }
    $.ajax({
        type: "post",
        url: "/LadderGroup/ChangeStatus",
        data: { id: id, status: status },
        success: function (res) {
            if (res.message && res.message == "Success") {
                alert("保存成功");
                refresh(1);
            } else {
                alert("保存失败");
                console.log(res.message);
            }
        }
    })
}

function refresh(pageIndex) {
        $.ajax({
            type: "GET",
            url: "/LadderGroup/GroupListPartial?pageIndex=" + parseInt(pageIndex) + "&pageSize=20" + "&q=" + "&status=" + $('#s_status').html(),
            data: {},
            success: function (data) {
                $("#list_table").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
}

function uploadImg(obj) {
    var selectedFile = ($(obj))[0].files[0];
    if (selectedFile) {
        var imageType = /image.*/;
        if (selectedFile.type.match(imageType) && selectedFile.size <= 2097152) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var dataURL = e.target.result;
                var img = new Image();
                img.src = dataURL;
                $(obj).siblings(".Add-ImgCuber").html(img);
            };
            reader.readAsDataURL(selectedFile);
        }
        else {
            if (!selectedFile.type.match(imageType)) {
                alert("只能录入图片文件！");
            } else if (selectedFile.size > 2097152) {
                alert("上传图片大小不能超过2Mb！");
            }
        }
    }
}
function submitGroup() {
    var values = [$("#pno").val(), $("#title").val(), $("#description").val(), $("#product_count").val(), $("#end_time").val()];
    var messages = ["请输入商品编号！", "请输入活动名称！", "请输入活动描述！", "请输入商品库存！", "请输入截止日期"];
    for (var i = 0; i < values.length; i++) {
        if (!values[i]) {
            alert(messages[i]);
            return;
        }
    }
    if (values[3] < 0) {
        alert("商品库存请输入正数！");
        return;
    }
    var pic = $(".Add-ImgCuber img").attr("src");
    if (!pic || pic == '/Content/images/pic.jpg') {
        alert("请选择活动图片！");
        return;
    }
    var json = {};
    json.pno = values[0];
    json.gid = $("#gid").val();
    json.title = values[1];
    json.description = values[2];
    json.pic = pic;
    json.waytoget = 0;
    json.product_count = values[3];
    json.end_time = new Date(values[4]).getTime() / 1000;
    var tar = $("#addJttBox .clearfix");
    if (tar.length <= 0) {
        alert("请添加成团人数和拼团价格");
        return;
    }
    var list = [];
    for (var i = 0; i < tar.length; i++) {
        var count = $(tar[i]).find(".person_count").val();
        var price = $(tar[i]).find(".group_price").val();
        if (isNaN(count) || isNaN(price)) {
            alert("请输入成团人数和拼团价格");
            return;
        }
        var ladderPrice = { person_count: count, group_price: parseInt(price * 100) };
        list.push(ladderPrice);
    }
    json.PriceList = list;
    $.ajax({
        url: '/LadderGroup/DoModify',
        type: "post",
        data: { group: json, type: $("#hid_type").val() },
        success: function (res) {
            if (res.status && res.status == true) {
                alert("保存成功");
                location = "/LadderGroup";
            } else if (res.status && res.status == "PnoError") {
                alert("输入的商品编号不存在!");
            } else if (res.status && res.status == "CanNotModified") {
                alert("活动已上线，不能编辑！");
            } else if (res.status && res.status == "UploadImgFail") {
                alert(res.message);
            } else if (res.status && res.status == "SessionTimeOut") {
                alert("登录超时，请重新登录！");
                location = "/";
            } else {
                alert("保存失败");
                console.log(res);
            }
        }
    })
}