(function (global) {
  const Bridge = {
    _handlers: {},
    _pending: {},
    _reqId: 0,

    send(cmd, data = null, needReply = false) {
      const msg = { cmd, data };
      if (needReply) {
        msg._reqId = ++this._reqId;
        return new Promise(resolve => {
          this._pending[msg._reqId] = resolve;
          window.chrome.webview.postMessage(msg); // 对象发送
        });
      }
      window.chrome.webview.postMessage(msg);
    },

    on(cmd, handler) {
      this._handlers[cmd] = handler;
    },

    _dispatch(msg) {
      if (msg._resId && this._pending[msg._resId]) {
        this._pending[msg._resId](msg.data);
        delete this._pending[msg._resId];
        return;
      }
      const handler = this._handlers[msg.cmd];
      if (handler) handler(msg.data);
    },

    _respond(resId, data) {
      window.chrome.webview.postMessage({ _resId: resId, data }); // ✅ 修复变量
    }
  };

  window.chrome.webview.addEventListener('message', e => {
    let msg = e.data;
    if (typeof msg === 'string') {
      try { msg = JSON.parse(msg); } catch {}
    }
    Bridge._dispatch(msg);
  });

  global.Bridge = Bridge;
})(window);
