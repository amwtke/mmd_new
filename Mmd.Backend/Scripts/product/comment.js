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

function refresh(pageCount) {
    var pid = $('#hid_pid').val();
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/Product/CommentListPartial?pid=" + pid + "&pageNo=" + parseInt(pageCount) + "&q=" + q,
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
//查看回复
function replyView(obj) {
    var tar = $(obj).parents("tr");
    var comment = tar.find(".comment").text();
    var reply = tar.find(".comment").data("reply");
    $('#tan_gname').val(comment);
    $('#tan_url').val(reply);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".inputBtn-Tap").hide();
    $(".i-close").on("click", function () {
        $(".alertForm,.maskLayer").css("display", "none");
        $('#tan_url').val("");
    });
}
//回复
function onReply(obj, id) {
    var comment = $(obj).parents("tr").find(".comment").text();
    $('#tan_gname').val(comment);
    $("#commentid").val(id);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".inputBtn-Tap").show();
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer").css("display", "none"); });
}
function postReply() {
    var reply = $('#tan_url').val();
    var id = $("#commentid").val();
    $.ajax({
        type: "POST",
        url: "/Product/ReplyComment",
        data: { pcid: id, reply: reply },
        success: function (data) {
            alert("保存成功！");
            //刷新到第一页
            refresh(1);
            $('#tan_gname').val("");
            $("#commentid").val("");
            $('#tan_url').val("");
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
    $(".alertForm,.maskLayer").css("display", "none"); 
}
//删除评论
function onDelClick(id) {
    if (confirm("确定要删除该评论吗？")) {
        $.ajax({
            type: "POST",
            url: "/Product/DelComment",
            data: { pcid: id },
            success: function (data) {
                alert("删除成功！");
                //刷新到第一页
                refresh(1);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    } 
}
function setTop(id, isessence) {
    $.ajax({
        type: "POST",
        url: "/Product/SetTop",
        data: { pcid: id, isessence: isessence },
        success: function (data) {
            alert("保存成功！");
            //刷新到第一页
            refresh(1);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
//搜索
function onSearch() {
    refresh(1);
}