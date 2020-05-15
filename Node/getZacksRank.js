//Except this npm package is a POS and only prints to the console.  So do not use this

module.exports = function (callback, symbol) {
    var exec = require('child_process').exec;
    const api = require('zacks-api');
    api.getData(symbol).then(console.log).then(exec(cmd, function (error, stdout) {
        callback(null, stdout);
    }));

    /*callback(null, stdout);
    exec(cmd, function (error, stdout) {
        callback(null, stdout);
    });
    callback(null, result);*/
};

function GetZacksRankFromStdout(cmd, callback) {
    exec(cmd, function (error, stdout) {
        return callback(null, stdout);
    });
}