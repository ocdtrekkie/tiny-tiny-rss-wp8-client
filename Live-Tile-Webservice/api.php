<?php

require 'Cipher.php';
// This is the API,  possibilities: add a user, update password, remove user or get unreadCount.
class TtRssAPI {

    private $db;
    private $cipher;

    // Constructor - open DB connection
    function __construct() {
        $this->cipher = new Cipher('54tj80561bfg98n136150f');
        $this->db = new \mysqli('localhost', 'ttrssapi', 'YT6TMbjVJBeKdN4j', 'ttrss-api');
    }

    // Destructor - close DB connection
    function __destruct() {
        $this->db->close();
    }

    function deleteUser($deviceId) {
        $result = $this->db->query("DELETE FROM users WHERE deviceId='$deviceId'");
        return $result;
    }

    function updateUser($deviceId, $loginName, $loginPassword, $server, $channel) {
        if ($this->db->connect_error) {
            return $this->db->connect_error;
        }
        $result = $this->db->query("SELECT COUNT(*) FROM users WHERE deviceId='$deviceId'");
        $row = $result->fetch_row();
        $pw = $this->cipher->encrypt($loginPassword);
        if ($row[0] > 0) {
            $rst = $this->db->query("UPDATE users SET userName='$loginName',password='$pw',server='$server',channel='$channel' "
                    . "WHERE deviceId='$deviceId'");
            return $rst;
        } else {
            $rst = $this->db->query("INSERT INTO users(userName,password,server,deviceId,channel,channelInactive) "
                    . "VALUES ('$loginName', '$pw', '$server', '$deviceId', '$channel', 0)");
            return $rst;
        }
    }

    function updateChannel($deviceId, $channel) {
        if ($this->db->connect_error) {
            return $this->db->connect_error;
        }
        $result = $this->db->query("SELECT COUNT(*) FROM users WHERE deviceId='$deviceId'");
        $row = $result->fetch_row();
        if ($row[0] > 0) {
            $rst = $this->db->query("UPDATE users SET channel='$channel' "
                    . "WHERE deviceId='$deviceId'");
            return $rst;
        }
        return 0;
    }
}

$possible_post = array("updateUser", "updateChannel", "deleteUser");
$value = 0;

$api = new TtRssAPI;
if (isset($_POST["action"]) && isset($_POST["hash"]) && $_POST["hash"] === "2u409g0hbinyv" && in_array($_POST["action"], $possible_post)) {
    switch ($_POST["action"]) {
        case "updateUser":
            $value = $api->updateUser($_POST["deviceId"], $_POST["loginName"], $_POST["loginPassword"], $_POST["server"], $_POST["channel"]);
            break;
        case "deleteUser":
            $value = $api->deleteUser($_POST["deviceId"]);
            break;
        case "updateChannel":
            $value = $api->updateChannel($_POST["deviceId"], $_POST["channel"]);
            break;
    }
}
exit($value);
