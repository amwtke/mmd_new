$(document).ready(function () {
    initUploadPics();
    initPics();
    initPrice();
    group_typeOnClick();
});
//价格初始化
function initPrice() {
    var price = $('#group_price').val();
    if (price != "") {
        price = parseInt($('#group_price').val());
        price = price / 100;
        $('#price_1').val(price);
    }
    var leader_price = $("#leader_price").val();
    if (leader_price != "") {
        leader_price = parseInt($('#leader_price').val());
        leader_price = leader_price / 100;
        $('#price_2').val(leader_price);
    }
    var commission = $("#Commission").val();
    if (commission) {
        $('#txt_Commission').val(commission/100);
    }
}


//初始化图片框
function initPics() {
    onLoadPics("dpic1", "ipic1", "advertise_pic_url", "thumb1");
}


/////////////////upload files

function initUploadPics() {
    $('#picg').change(function (evt) { fileSelected(evt, "dpic1", "ipic1", "picg", "advertise_pic_url", "thumb1"); });
}

function fileSelected(evt, divId, iId, inputId, textBoxId, thumbId) {
    var selectedFile = ($('#' + inputId))[0].files[0];//FileControl.files[0];

    if (selectedFile) {
        var imageType = /image.*/;

        if (selectedFile.type.match(imageType) && selectedFile.size <= 2097152) {
            var reader = new FileReader();
            reader.onload = function (e) {

                $("#" + thumbId).remove();//删除缩略图
                $("#" + iId).remove();//删除加号
                var dataURL = reader.result;
                var img = new Image();
                img.src = dataURL;
                img.className = "thumb";
                img.id = thumbId;
                $("#" + divId).append(img);
            };
            reader.readAsDataURL(selectedFile);
            $('#' + textBoxId).val(selectedFile.name);
        } else {
            if (!selectedFile.type.match(imageType)) {
                alert("只能录入图片文件！");
                $('#' + inputId).val("");
            }

            if (selectedFile.size > 2097152) {
                alert("上传图片大小不能超过2Mb！");
                $('#' + inputId).val("");
            }
            $('#' + textBoxId).val("");
            $("#" + thumbId).remove();//删除缩略图
            //如果没有加号则加上，防止多加
            if ($('#' + iId).length === 0) {
                $("#" + divId).append("<i id=" + iId + " class=\"MMda i-add pdr5 green\"></i>");
            }
        }
    }
}

function onLoadPics(divId, iId, textBoxId, thumbId) {
    var imgName = $('#' + textBoxId).val();
    if (imgName) {
        if (imgName.indexOf("mmpintuan") > 0) {
            $("#" + iId).remove();//删除加号
            $("#" + divId).append("<img src=\"" + imgName + "\" id=\"" + thumbId + "\" class=\"thumb\">");//添加图片链接
        }
    }
}

//提交以及验证
function htmlEncode(value) {
    return $('<div/>').text(value).html();
}
function submitFormGroup() {
    if (validation()) {
        var title = htmlEncode($('#title').val());
        $('#title').val(title);

        var des = htmlEncode($('#description').val());
        $('#description').val(des);

        $("#group_from").submit();
    }
    else {
        showErrors();
    }
}
//拼团价格验证
function validate_price() {
    var price = $('#price_1').val();
    var patt = new RegExp(/^\+?(?!0+(\.00?)?$)\d+(\.\d\d?)?$/);
    if (price.match(patt)) {
        price = parseFloat(100 * price).toFixed(0);
        $('#group_price').val(price);
        return true;
    }
    return false;
}
//团长优惠价格验证
function validate_leader_price() {
    var leader_price = $("#price_2").val();
    var patt = new RegExp(/^[0-9]+([.][0-9]+){0,1}$/);
    if (leader_price.match(patt)) {
        leader_price = parseFloat(leader_price);
        leader_price = parseFloat(100 * leader_price).toFixed(0);
        $('#leader_price').val(leader_price);
        return true;
    }
    return false;
}
//团长优惠价小于等于拼团价格
function validate_leader_price_group() {
    var group_price = $("#group_price").val();
    var leader_price = $("#leader_price").val();
    if (group_price != "" && leader_price != "") {
        group_price = parseInt(group_price);
        leader_price = parseInt(leader_price);
        if (leader_price < group_price) {
            return true;
        }
    }
    return false;
}
//分销佣金验证
function validate_Commission() {
    var Commission = $("#txt_Commission").val();
    if (!isNaN(Commission) && parseFloat(Commission *100) >= 1) {
        $("#spanCommission").hide();
        $('#Commission').val(parseFloat(100 * Commission).toFixed(0));
        return true;
    } else {
        $("#spanCommission").show();
        return false;
    }
}
//商品库存验证，必须为成团人数的整数倍
function validate_product_quota() {
    var person_quota = $("#person_quota").val();
    var product_quota = $("#product_quota").val();
    if (person_quota != "" && product_quota != "") {
        person_quota = parseInt(person_quota);
        product_quota = parseInt(product_quota);
        return true;
        //if (product_quota % person_quota == 0) {
        //    return true;
        //}
    }
    return false;
}
//验证订单限制购买次数必填
function validate_order_limit() {
    var txtlimit = $("#order_limit").val();
    var patt = new RegExp(/^[0-9]\d*$/);
    if (txtlimit.match(patt)) {
        txtlimit = parseInt(txtlimit);
        return true;
    }
    return false;
}
//验证活动门店为必选
function validate_activity_point() {
    var checkedCount = $("input[name='activity_point']:checked").length;
    return checkedCount > 0;
}
//验证设置运费模板
function validate_ltid() {
    var val = $('input:radio[name="waytoget"]:checked').val();
    if (val == 1 || val == 2) {
        var ltid = $("#ltid").val();
        if (ltid) {
            $("#ltidMsg").hide();
            return true;
        } else {
            $("#ltidMsg").show();
            return false;
        }
    } else {
        $("#ltidMsg").show();
        return true;
    }
}
function validation() {
    if (!validate_price())
        return false;
    if (!validate_leader_price())
        return false;
    if (!validate_Commission()) {
        return false;
    }
    if (!validate_leader_price_group())
        return false;
    if (!validate_product_quota())
        return false;
    if ($('#advertise_pic_url').val() === "")
        return false;
    if (!validate_order_limit())
        return false;
    if (!validate_activity_point())
        return false;
    if (!validate_ltid())
        return false;
    //如果没有错误
    $("#spanpics").css('display', 'none');
    $("#spanprice").css('display', 'none');
    $("#spanproduct_quota").css('display', 'none');
    $("#spanleader_price").css('display', 'none');
    $("#spanleader_price2").css('display', 'none');
    $("#span_order_limit").css('display', 'none');
    $("#span_activitypoint").css('display', 'none');
    return true;
}

function showErrors() {
    //价格显示错误
    var flag = true;
    if (!validate_price()) {
        $('#spanprice').removeAttr("style");
    } else {
        $("#spanprice").css('display', 'none');
    }
    //团长优惠显示错误
    if (!validate_leader_price()) {
        $('#spanleader_price').removeAttr("style");
    } else {
        $("#spanleader_price").css('display', 'none');
        flag = false;//说明团长优惠验证成功，此时再验证是否<=拼团价格
    }
    if (!flag) {
        if (!validate_leader_price_group()) {
            $('#spanleader_price2').removeAttr("style");
        } else {
            $("#spanleader_price2").css('display', 'none');
        }
    }

    //商品库存量是否为成团人数的整数倍
    if (!validate_product_quota()) {
        $('#spanproduct_quota').removeAttr("style");
    } else {
        $("#spanproduct_quota").css('display', 'none');
    }
    //订单限制购买次数格式验证
    if (!validate_order_limit()) {
        $('#span_order_limit').removeAttr("style");
    } else {
        $("#span_order_limit").css('display', 'none');
    }
    if (!validate_activity_point()) {
        $("#span_activitypoint").removeAttr("style");
    } else {
        $("#span_activitypoint").css('display', 'none');
    }
    flag = true;
    //图片显示错误
    if ($('#advertise_pic_url').val() === "")
        flag = false;

    if (!flag) {
        $("#spanpics").removeAttr("style");
    }
    else {
        $("#spanpics").css('display', 'none');
    }
}

function group_typeOnClick() {
    var value = $('input[name="group_type"]:checked').val();
    if (value == 0) {
        $('input[name="userobot"]').removeAttr("disabled");
    }
    else {
        $('input[name="userobot"]').attr("disabled", "disabled")
    }
}

//下一步进入抽奖团提交
function submitFormGroup_lucky() {
    if (lucky_Valida()) {
        $("#grouplucky_from").submit();
    }
}
function lucky_Valida() {
    if (!luckyCount_Valida())
        return false;
    if (!luckyEndTime_Valida())
        return false;
    return true;
}
//验证中奖人数
function luckyCount_Valida() {
    var luckycount = $("#lucky_count").val();
    var patt = new RegExp(/^[1-9]\d*$/);
    if (luckycount.match(patt)) {
        $("#spanluckycounts").css('display', 'none');
        return true;
    }
    else {
        $('#spanluckycounts').removeAttr("style");
        return false;
    }
}
//验证结束时间
function luckyEndTime_Valida() {
    var luckyendtime = $("#lucky_endTime").val();
    if (luckyendtime != "") {
        $("#spanluckyendTime").css('display', 'none');
        //$("#lucky_endTime").val(luckyendtime);
        return true;
    } else {
        $('#spanluckyendTime').removeAttr("style");
        return false;
    }
}