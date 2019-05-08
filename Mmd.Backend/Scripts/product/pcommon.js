$(document).ready(function() {
    initUploadPics();
    initKindEditor();
    initPics();
    initPrice();
});
//价格初始化
function initPrice() {
    var price = $('#price').val();
    if (price.length !== 0) {
        price = parseInt($('#price').val());
        price = price / 100;
        $('#price1').val(price);
    }
}


//////初始化kindeditor
function htmlEncode(value) {
    return $('<div/>').text(value).html();
}
//Html解码获取Html实体
function htmlDecode(value) {
    return $('<div/>').html(value).text();
}
//提交模式
function onFormSubmit() {
    editor.sync();
    if ($('#description2').val().trim() === "") {
        $('#description').val("");
    } else {
        var html = htmlEncode($('#description2').val());
        $('#description').val(html);
    }
    //alert($('#description').val());
}
//edit模式
function initKindEditor() {
    var html = $('#description').val();
    if (html) {
        html = htmlDecode(html);//解码
        $('#description2').val(html);
    }
}

//初始化图片框
function initPics() {
    onLoadPics("dpic1", "ipic1", "advertise_pic_1", "thumb1");
    onLoadPics("dpic2", "ipic2", "advertise_pic_2", "thumb2");
    onLoadPics("dpic3", "ipic3", "advertise_pic_3", "thumb3");
}


/////////////////upload files

function initUploadPics() {
    $('#pic1').change(function (evt) { fileSelected(evt, "dpic1", "ipic1", "pic1", "advertise_pic_1", "thumb1"); });
    $('#pic2').change(function (evt) { fileSelected(evt, "dpic2", "ipic2", "pic2", "advertise_pic_2", "thumb2"); });
    $('#pic3').change(function (evt) { fileSelected(evt, "dpic3", "ipic3", "pic3", "advertise_pic_3", "thumb3"); });
}

function fileSelected(evt,divId,iId,inputId,textBoxId,thumbId) {
    //var selectedFile = evt.target.files can use this  or select input file element and access it's files object
    var selectedFile = ($('#'+inputId))[0].files[0];//FileControl.files[0];

    if (selectedFile) {
        var imageType = /image.*/;

        if (selectedFile.type.match(imageType) && selectedFile.size <= 2097152) {
            var reader = new FileReader();
            reader.onload = function (e) {

                $("#" + thumbId).remove();//删除缩略图
                $("#"+iId).remove();//删除加号
                var dataURL = reader.result;
                var img = new Image();
                img.src = dataURL;
                img.className = "thumb";
                img.id = thumbId;
                $("#"+divId).append(img);
            };
            reader.readAsDataURL(selectedFile);
            $('#'+textBoxId).val(selectedFile.name);
        } else {
            if (!selectedFile.type.match(imageType)) {
                alert("只能录入图片文件！");
                $('#' + inputId).val("");
            }else if (selectedFile.size > 2097152) {
                alert("上传图片大小不能超过2Mb！");
                $('#' + inputId).val("");
            }

            $('#' + textBoxId).val("");
            $("#" + thumbId).remove();//删除缩略图
            //如果没有加号则加上，防止多加
            if ($('#'+iId).length === 0) {
                $("#"+divId).append("<i id="+iId+" class=\"MMda i-add pdr5 green\"></i>");
            }
        }
    }
}

function onLoadPics(divId, iId, textBoxId,thumbId) {
    var imgName = $('#' + textBoxId).val();
    if (imgName) {
        if (imgName.indexOf("mmpintuan") > 0) {
            $("#" + iId).remove();//删除加号
            $("#"+divId).append("<img src=\"" + imgName + "\" id=\""+thumbId+"\" class=\"thumb\">");//添加图片链接
        }
    }
}

//提交以及验证
function submitForm() {
    onFormSubmit();
    if (validation())
        $('#edit_form').submit();
    else {
        showErrors();
    }
}

function validate_price() {
    var price = $('#price1').val();
    var patt = new RegExp(/^\+?(?!0+(\.00?)?$)\d+(\.\d\d?)?$/);
    if (price.match(patt)) {
        price = parseFloat(price);
        price = parseFloat(100 * price).toFixed(0);
        $('#price').val(price);
        return true;
    }
    return false;
}

function validation() {
    if (!validate_price())
        return false;
    if ($('#advertise_pic_1').val() === "")
        return false;
    if ($('#advertise_pic_2').val() === "")
        return false;
    if ($('#advertise_pic_3').val() === "")
        return false;
    if ($('#description').val() === "")
        return false;

    $("#spanintro").css('display', 'none');
    $("#spanpics").css('display', 'none');
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

    //图片显示错误
    if ($('#advertise_pic_1').val() === "")
        flag = false;
    if ($('#advertise_pic_2').val() === "")
        flag = false;
    if ($('#advertise_pic_3').val() === "")
        flag = false;
    if (!flag) {
        $("#spanpics").removeAttr("style");
    }
    else {
        $("#spanpics").css('display', 'none');
    }

    //描述显示错误
    if ($('#description').val() === "")
        $("#spanintro").removeAttr("style");
    else {
        $("#spanintro").css('display', 'none');
    }
}