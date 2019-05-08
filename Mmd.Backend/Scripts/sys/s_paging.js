
//////标签展示
function onM() {//所有订单
    removeAllCurs();
    $('#label_m').addClass("cur");
    $('#s_tag').html("m");
   
    refresh_tag();
}

function onW() {//待提货
    removeAllCurs();
    $('#label_w').addClass("cur");
    $('#s_tag').html("w");
    refresh_tag();
}

function onManage() {
    removeAllCurs();
    $('#label_mana').addClass("cur");
    $.ajax({
        type: "GET",
        url: "/System/GetAllWoper?q=",
        data: {},
        success: function (data) {
            $("#tag_content").html(data);
            
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function removeAllCurs() {
    $('#label_m').removeClass("cur");
    $('#label_w').removeClass("cur");
    $("#label_mana").removeClass("cur");
    //$('#label_yth').removeClass("cur");
}

function refresh_tag() {
    var tag = $('#s_tag').html();
    var tagValue = 0;
    if (tag === "w")
        tagValue = 1;
    if (tag == 'm')
        tagValue = 0;

    $.ajax({
        type: "GET",
        url: "/System/SwichTag?tag=" + tagValue,
        data: {},
        datatype: "json",
        success: function(data) {
            $("#tag_content").html(data);
            if (tagValue === 0) {
                initPics();
            }
        },
        error: function(XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
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

    $.ajax({
        type: "GET",
        url: "/System/SwichTag?tag=1&q=" + q + "&indexPage=" + pageIndex,
        data: {},
        datatype: "json",
        success: function(data) {
            $("#tag_content").html(data);
        },
        error: function(XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });


}

function onSearch() {
    refresh(1);
}

//添加提货点
function onAddWOP() {
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/System/SwichTag?tag=3&q="+q,
        data: {},
        datatype: "json",
        success: function (data) {
            $("#tag_content").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
//编辑提货点
function onEditWOP(id) {
    var woid = $('#woid_' + id).text();
    var q = $('#s_text').val();
    $.ajax({
        type: "GET",
        url: "/System/SwichTag?tag=3&woid=" + woid+"&q="+q,
        data: {},
        datatype: "json",
        success: function (data) {
            $("#tag_content").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}

//删除提货点
function onDelWop(id,obj) {
    var woid = $('#woid_' + id).text();
    var name = $('#name_' + id).text();
    var q = $('#s_text').val();
    var count = $(obj).parents("tr").find(".wopercount").text();
    count = parseInt(count);
    var message = '是否要删除?\n' + name;
    if (count > 0) {
        message = '该提货点有'+count+'个核销员，确定要一起删除吗？';
    }
    if (!confirm(message)) {
        return false;
    } else {
        $.ajax({
            type: "GET",
            url: "/System/DelWop?woid=" + woid+"&q="+q,
            data: {},
            datatype: "json",
            success: function (data) {
                $("#tag_content").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}

//添加核销员
function onAddWoer(id) {
    var woid = $('#woid_' + id).text();
    $.ajax({
        type: "GET",
        url: "/System/SwichTag?tag=2&woid=" + woid,
        data: {},
        datatype: "json",
        success: function (data) {
            $("#tag_content").html(data);
            initQr();
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}

//删除woer
function onDelWoer(id) {
    var woid = $('#woer_woid').text();
    var woerid = $('#woerid_' + id).text();
    var name = $('#woer_name_' + id).text();
    if (!confirm("是否要删除?\n" + name)) {
        return false;

    } else {
        $.ajax({
            type: "GET",
            url: "/System/DeleteWoer?woerid=" + woerid + "&woid=" + woid,
            data: {},
            datatype: "json",
            success: function (data) {
                $("#tag_content").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}
function onDelWoper(obj) {
    var woid = $(obj).parent().data("woid");
    var woerid = $(obj).parent().data("woerid");
    if (!confirm("是否要删除该核销员?\n")) {
        return false;
    } else {
        $.ajax({
            type: "GET",
            url: "/System/DeleteWoer?woerid=" + woerid + "&woid=" + woid,
            data: {},
            success: function (data) {
                $(obj).parents("tr").remove();
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}
function encode(s) {
    return s.replace(/&/g, "&").replace(/</g, "<").replace(/>/g, ">").replace(/([\\\.\*\[\]\(\)\$\^])/g, "\\$1");
}
function decode(s) {
    return s.replace(/\\([\\\.\*\[\]\(\)\$\^])/g, "$1").replace(/>/g, ">").replace(/</g, "<").replace(/&/g, "&");
}
function highlight() {
    var s = $("#s_text").val();
    if (s.length == 0) {
        alert('搜索关键词未填写！');
        return false;
    }
    s = encode(s);
    var obj = $(".Lorelei-commonTable")[0];
    var t = obj.innerHTML.replace(/<span\s+class=.?highlight.?>([^<>]*)<\/span>/gi, "$1");
    obj.innerHTML = t;
    var cnt = loopSearch(s, obj);
    t = obj.innerHTML
    var r = /{searchHL}(({(?!\/searchHL})|[^{])*){\/searchHL}/g
    t = t.replace(r, "<span class='highlight'>$1</span>");
    obj.innerHTML = t;
    if (cnt > 0) {
        alert("搜索到" + cnt + "处关键词");
        var mTop = $('.highlight').eq(0).offset().top;
        if (mTop > 200) {
            window.scrollTo(0, mTop - 200);
        }
    } else {
        alert("未搜索到关键词");
    }
}
function loopSearch(s, obj) {
    var cnt = 0;
    if (obj.nodeType == 3) {
        cnt = replace(s, obj);
        return cnt;
    }
    for (var i = 0, c; c = obj.childNodes[i]; i++) {
        if (!c.className || c.className != "highlight")
            cnt += loopSearch(s, c);
    }
    return cnt;
}
function replace(s, dest) {
    var r = new RegExp(s, "g");
    var tm = null;
    var t = dest.nodeValue;
    var cnt = 0;
    if (tm = t.match(r)) {
        cnt = tm.length;
        t = t.replace(r, "{searchHL}" + decode(s) + "{/searchHL}")
        dest.nodeValue = t;
    }
    return cnt;
}