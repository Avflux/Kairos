window.kanbanDragDrop = {
    dragData: {},

    setData: function (format, data) {
        this.dragData[format] = data;
    },

    getData: function (format) {
        return this.dragData[format] || '';
    },

    clearData: function () {
        this.dragData = {};
    }
};