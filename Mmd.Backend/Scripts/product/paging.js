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
    var appid = $('#appid').text();
    var q = $('#s_text').val();
    if (q.length === 0) {
        $.ajax({
            type: "GET",
            url: "/Product/ProductListPartial?pageNo=" + parseInt(pageCount) + "&q=",
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
            url: "/Product/ProductListPartial?pageNo=" + parseInt(pageCount) + "&q=" + q,
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

/////////////////////////删除商品
function onDelClick(id) {
    var pid = $('#pid_' + id).text();
    var name = $('#name_' + id).text();
    var pno = $('#pno_' + id).text();
    var size = $('#size_' + id).text();
    size = parseInt(size);

    if (!confirm("是否要删除?\n商品名:" + name + " \n商品编号:" + pno)) {
        return false;

    } else {
        delProduct(pid);
        return true;

    }
}

function onCreateGroup(id) {
    var pid = $('#pid_' + id).text();
    var name = $('#name_' + id).text();
    var pno = $('#pno_' + id).text();

    //查询剩余订单数，判断是否可以创建拼团
    $.ajax({
        async: false,
        type: "GET",
        url: "/Group/CanAddGroup",
        data: {},
        datatype: "json",
        success: function (data) {
            if (data == "True") {
                if (!confirm("是否要为:\n商品名:" + name + " \n商品编号:" + pno + "\n创建拼团？")) {
                    return false;
                } else {
                    createGroup(pid);
                    return true;
                }
            }
            else {
                alert('您好，贵公司剩余的拼团订单额不足，无法创建新的拼团活动，请联系工作人员充值，联系电话：18108611928！');
                return false;
            }
        }
    });
}

function createGroup(pid) {
    window.location.href = "/Group/AddGroup?pid=" + pid;
}

function delProduct(pid) {
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/Product/DelProduct?pid=" + pid + "&q=" + q,
        data: {},
        datatype: "json",
        success: function (data) {
            alert("删除成功！");

            //刷新到第一页
            $("#list_table").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
//显示推广链接
function onTuiguang(obj, title) {
    var url = $(obj).data("prourl");
    $('#tan_gname').val(title);
    $('#tan_url').text(url);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer").css("display", "none"); });
}
/////////搜索
function onSearch() {
    refresh(1);
}