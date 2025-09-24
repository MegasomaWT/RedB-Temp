let _propertiesSource = [];
let pRowKey = 'properties';
let _propertiesGrid = $("#propertiesGrid").dxDataGrid({
    dataSource: _propertiesSource,
    keyExpr: 'key',
    scrolling: {
        useNative: true,
        scrollByContent: true,
        scrollByThumb: true,
        showScrollbar: "onHover", // or "onScroll" | "always" | "never"
        mode: "standard" // or "virtual"
    },
    editing: {
        mode: 'cell',
        allowUpdating: true
    },
    columns: [
        { dataField: "key", headerCellTemplate: $('<span style="color: black">key</span>'), allowEditing: false },
        { dataField: "value", headerCellTemplate: $('<span style="color: black">value</span>'), allowEditing: true }
    ],
    onCellPrepared(e) {
        if (e.key == pRowKey && e.columnIndex==1)
            e.cellElement[0].textContent = '';
    },
    onEditingStart(e) {
        if (e.column.index == 1 && e.key == pRowKey) e.cancel = true;
    },
    onSelectionChanged(e) {
        e.component.collapseAll(-1);
        e.component.expandRow(e.currentSelectedRowKeys[0]);
    },
    onContentReady(e) {
        if (!e.component.getSelectedRowKeys().length) { e.component.selectRows([pRowKey], false); }
    },
    masterDetail: {
        enabled: false,
        template(container, options) {
            $('<div>')
                .dxDataGrid({
                    showColumnHeaders: true,
                    showBorders: true,
                    editing: {
                        mode: 'cell',
                        allowUpdating: true
                    },
                    columns: [
                        { dataField: "name", headerCellTemplate: $('<span style="color: black">key</span>'), allowEditing: false },
                        { dataField: "value", headerCellTemplate: $('<span style="color: black">value</span>'), allowEditing: true }
                    ],
                    dataSource: new DevExpress.data.DataSource({
                        store: new DevExpress.data.ArrayStore({ data: options.data.value }),
                    }),
                }).appendTo(container);
        }
    }
}).dxDataGrid("instance");

function openProperties(e) {
    let d = $.Deferred();
    $.getJSON('/Cnt/' + e.type + '/details?id=' + e.id)
        .done(function (result) {
            d.resolve(result);
            _propertiesSource = [];
            Object.entries(result).forEach(([key, value]) => _propertiesSource.push({ key: key, value: value }));
            _propertiesGrid.option('dataSource', _propertiesSource);
        })
        .fail(function () {
            throw 'Data loading error';
        });
    $('#propertiesHeaderText').text('Item id = ' + e.id + ' information');
}



