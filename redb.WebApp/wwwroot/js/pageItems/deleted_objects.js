$(() => {

    $("#DeletedObjectsList").dxTreeList({
        scrolling: {
            useNative: true,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never"
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON("/Cnt/CRDeletedObjects/GetAllObjects")
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw "Data loading error";
                    });
            }
        }),
        sorting: {
            mode: "multiple",
        },
        selection: {
            mode: "single",
        },
        columns: [{
            dataField: "id",
            caption: "id",
            fixed: true,
            cellTemplate: function (container, options) {
                let refProperties = $("<a>", { class: "propertiesToggle", text: options.data.id, id: options.data.id, type: "CRDeletedObjects" });
                refProperties.one("click", handler1);
                container.append(refProperties);
            }
        },
        {
            dataField: "name",
            caption: "Name"
        },
        {
            dataField: "parentId",
            caption: "Parent"
        }]
    });
});