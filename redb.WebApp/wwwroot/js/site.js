$(() => {

    const lu = (aPrefix) => {
        switch (aPrefix) {
            case 'xhtml': return 'http://www.w3.org/1999/xhtml';
            case 'math': return 'http://www.w3.org/1998/Math/MathML';
            case 'svg': return 'http://www.w3.org/2000/svg';
        }
        return '';
    }

    const itemTemplate = (data) => {
        const sidebar_item = $('<div>').addClass('sidebar-item');
        const sidebar_item_svg = $('<div>').addClass('sidebar-item-svg').appendTo(sidebar_item);
        const svg = $(document.createElementNS(lu('svg'), 'svg')).appendTo(sidebar_item_svg);
        $(document.createElementNS(lu('svg'), 'path')).attr('d', data.path).appendTo(svg);
        $('<span>').text(data.name).appendTo(sidebar_item);
        return sidebar_item;
    }

    const switchByName = (e) => {
        $.post("/Cnt/SidebarList/SetSidebarListIndex?itemIndex=" + e.itemIndex);
        let name = e.itemData.name.replace(" ", "_").toLowerCase();
        switch (name) {
            case "objects": location.href = '/pageitems/objects'; break;
            case "deleted_objects": location.href = '/pageitems/deleted_objects'; break;
            default: location.href = '/index'; break;
        }
    }

    $('#homeList').dxList({
        itemTemplate: itemTemplate,
        items: [
            {
                name: 'Nodes',
                path: 'M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z'
            }
        ],
        onItemClick: function (e) {
            location.href = '/index';
        }
    });

    $('#generalList').dxList({
        height: '100%',
        itemTemplate: itemTemplate,
        scrolling: {
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON('/Cnt/SidebarList/General')
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw 'Data loading error';
                    });
            }
        }),
        selectionMode: 'single',
        onItemClick: switchByName,
        onContentReady: function (e) {
            let index = $("#modal").attr("SidebarListIndex");
            e.component.selectItem(parseInt(index));
        }
    });

    $('#globalList').dxList({
        itemTemplate: itemTemplate,
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON('/Cnt/SidebarList/Global_Settings')
                    .done(function (result) {
                        $('body').css('display', 'block');
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw 'Data loading error';
                    });
            }
        })
    });
});


function handler1(e) {
    e.stopPropagation();
    $('.properties').addClass('openRSideBar').animate({ right: '0px' });
    $(this).addClass('openRSideBar');
    $(this).off("click", handler1).on("click", handler2);
    openProperties(e.target);
    $('#modal').show();
}

function handler2(e) {
    e.stopPropagation();
    $('.properties').removeClass('openRSideBar').animate({ right: '-100%' });
    $('.propertiesToggle').removeClass('openRSideBar');
    $('.propertiesToggle').off("click", handler2).on("click", handler1);
    $('#modal').hide();
}


$(document).ready(function () {
    $('#logout').click(function () {
        $('#form-logout').submit();
    });

    if (!Boolean(document.getElementById('isAtGeneralList')))
        $('#modal').removeAttr('SidebarListIndex');

    $('#modal').click(function (event) {
        if (!$(event.target).closest('.properties.openRSideBar').length) {
            if ($('.properties').hasClass("openRSideBar")) {
                $('.properties').removeClass('openRSideBar').animate({ right: '-100%' });
                $(".propertiesToggle").on("click", handler1);
                $('#modal').hide();
            }
        }
    });

});


const nodesSource = [{'Name':'16A846BB-163D-44EC-9453-F87827E5CEEC'}];
//let list;

$(function () {
    const stylingMode = 'outlined';
    const labelMode = 'static';
    DevExpress.config({
        editorStylingMode: stylingMode,
    });

    $("#nodes").dxSelectBox({
        displayExpr: 'Name',
        dataSource: nodesSource,
        height: '30px',
        label: 'nodes',
        labelMode: 'floating',
        value: nodesSource[0],
        onValueChanged(data) {
        }
    })

//    $("#nodes").dxDropDownBox({
//        acceptCustomValue: true,
//        openOnFieldClick: false,
//        acceptCustomValue: false,
//        value: dataSource[0],
//        dataSource,
//        height: '30px',
//        label: 'nodes',
//        labelMode: 'floating',
//        contentTemplate: function (e) {
//            const $list = $("<div>").dxList({
//                dataSource,
//                selectionMode: "single",
//                onSelectionChanged: function (arg) {
//                    e.component.option("value", arg.addedItems[0]);
//                    e.component.close();
//                }
//            });
//            list = $list.dxList('instance');
//            return $list;
//        },
//    });

//    list.selectItem(0);
});
