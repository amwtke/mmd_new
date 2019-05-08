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
    var gid = $("#gid").val();
    if (gid.length === 0)
        return;
    $.ajax({
        type: "GET",
        url: "/Group/GroupGetDetailPartial?pageIndex=" + parseInt(pageIndex) + "&gid=" + gid,
        data: {},
        datatype: "json",
        success: function(data) {
            $("#list_table").html(data);
        },
        error: function(XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}