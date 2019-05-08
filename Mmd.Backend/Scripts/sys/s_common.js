$(document).ready(function () {
    initUploadPics();
    initPics();
});

///核销二维码
function initQr() {
    var url = $('#input_url').val();
    $('#woer_qrcode').qrcode(url);
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

        if (selectedFile.type.match(imageType)) {
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
            alert("只能录入图片文件！");
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

function submitSystemForm() {
    if (validation()) {
        $("#m_form").submit();
    }
}

function validation() {
    return showErrors();
}

function showErrors() {

    return errorSwitch('title', 'span_title') &&
    errorSwitch('slogen', 'span_slogen') &&
    errorSwitch('advertise_pic_url', 'spanpics') &&
    errorSwitch('brief_introduction', 'span_brief_introduction') &&
    errorSwitch('service_intro', 'span_service_intro');
}

function errorSwitch(id,spanid) {
    if ($('#'+id).val() === "") {
        $("#" + spanid).removeAttr("style");
        return false;
    } else {
        $("#" + spanid).css('display', 'none');
        return true;
    }
}


//添加wop的校验
function submitWopAddForm() {
    if (validation_wop()) {
        $("#wop_form").submit();
    }
}

function validation_wop() {
    return showErrors_wop();
}
function showErrors_wop() {

    return errorSwitch('name', 'span_wopname') &&
        errorSwitch('address', 'span_wopaddress') &&
        errorSwitch('tel', 'span_woptel');
}