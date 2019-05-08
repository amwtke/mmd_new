
function fileSelectedChange(evt, divId, iId, inputId, textBoxId, thumbId) {
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
function onLoadPic(divId, iId, textBoxId, thumbId) {
    var imgName = $('#' + textBoxId).val();
    if (imgName) {
        if (imgName.indexOf("mmpintuan") > 0) {
            $("#" + iId).remove();//删除加号
            $("#" + divId).append("<img src=\"" + imgName + "\" id=\"" + thumbId + "\" class=\"thumb\">");//添加图片链接
        }
    }
}

function submitFormNoticeBoard() {
    onFormSubmit();
    if (validation()) {
        $("#noticeboard_from").submit();
    }
    else {
        showErrors();
    }
}
function validation() {
    if ($("#thumb_pic").val() == "") {
        return false;
    }
    $("#spanpics").css('display', 'none');
    return true;
}
function showErrors() {
    if ($("#thumb_pic").val() == "") {
        $("#spanpics").removeAttr("style");
    }
    else {
        $("#spanpics").css('display', 'none');
    }
}

//提交模式
function onFormSubmit() {
    debugger;
    editor.sync();
    if ($('#description2').val().trim() === "") {
        $('#description').val(" ");//给空时会保存为null，数据不更新，所以赋值空格
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
//////初始化kindeditor
function htmlEncode(value) {
    return $('<div/>').text(value).html();
}
//Html解码获取Html实体
function htmlDecode(value) {
    return $('<div/>').html(value).text();
}

//翻页按钮（上一页）
function onLeftClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    if (currentPageNumber === 1)
        alert("已经是第1页了！");
    else {
        refresh(--currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}
//翻页按钮（下一页）
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
//翻页按钮（跳转）
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
//刷新列表
function refresh(pageIndex) {
    var status = $("#hidden_status").val();
    var q = $("#s_searchText").val();
    $.ajax({
        type: "GET",
        url: "/NoticeBoardMana/GetList?pageIndex=" + parseInt(pageIndex) + "&q=" + q + "&status=" + status,
        data: {},
        datatype: "json",
        success: function (data) {
            $(".Lorelei-commonTable").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function DelNoticeBoard(nid, status) {
    $.ajax({
        type: "GET",
        url: "/NoticeBoardMana/DelNoticeBoard?nid=" + nid + "&status=" + status,
        data: {},
        datatype: "json",
        success: function (data) {
            if (data == "True") {
                alert('操作成功');
                refresh(1);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function SetTop(nid) {
    $.ajax({
        type: "POST",
        url: "/NoticeBoardMana/SetTop",
        data: { nid: nid, operation:"settop"},
        datatype: "json",
        success: function (data) {
            if (data && data.Result == "Success") {
                alert('操作成功');
                refresh(1);
            } else {
                alert('操作失败');
                console.log(data.Message);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            console.log(errorThrown);
        }
    });
}
function sendMessage(id) {
    if (!confirm("确定要推送该资讯吗？")) {
        return;
    }
    $.ajax({
        type: "POST",
        url: "/NoticeBoardMana/SendArticle",
        data: { nid: id},
        success: function (data) {
            if (data && data.Result == "Success") {
                alert('推送成功');
            } else {
                alert('推送失败');
                console.log(data.Message);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            console.log(errorThrown);
        }
    });
}