<?php

// This is the API,  possibilities: add a user, update password, remove user or get unreadCount.
class TtRssAPI
{
    private $db;

    // Constructor - open DB connection
    function __construct()
    {
        $this->db = new mysqli('localhost', 'mysqluser', 'mysqlpassword', 'ttrss-api');
    }

    // Destructor - close DB connection
    function __destruct()
    {
        $this->db->close();
    }

    function getUnreadCount($deviceId)
    {
        $result = $this->db->query("SELECT * FROM users WHERE deviceId='$deviceId'");
        //return "test1";

        if (!$result) {
            return 0;
        }
        /* fetch object array */
        $row = $result->fetch_object();
        $server = $row->server;
        $userName = $row->userName;
        $password = $row->password;
        /* free result set */
        $result->close();
        /* login to tt-rss */
        $loginData = json_decode($this->ttrsscurl($server, '{"op":"login","user":"'.$userName.'","password":"'.$password.'"}'), TRUE);
        if($loginData == null || $loginData[status] == 1) {
            return 0;
        }
        $sessionId = $loginData[content][session_id];
        /* get counters */
        $counters = json_decode($this->ttrsscurl($server, '{"op":"getCounters","sid":"'.$sessionId.'","output_mode":"f"}'), TRUE);
        if($counters[status] == 1) {
            return 0;
            $this->ttrsscurl($server, '{"op":"logout","sid":"'.$sessionId.'"}');
        }
        $unreadCount = 0;
        $counterContent = $counters[content];
        foreach ($counterContent as $counter) {
            if($counter[id]==-3) {
                $unreadCount = $counter[counter];
                break;
            }
        }
        /* logout */
        $this->ttrsscurl($server, '{"op":"logout","sid":"'.$sessionId.'"}');
        return $unreadCount;
    }

    function ttrsscurl($url, $params)
    {
        $ch = curl_init();
        $defaults = array(
            CURLOPT_POST => 1,
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => 1,
            CURLOPT_SSL_VERIFYPEER => false
        );
        curl_setopt($ch, CURLOPT_HTTPHEADER, array(
                'Content-Type: application/json',
                'Content-Length: ' . strlen($params))
        );
        $ch = curl_init();
        curl_setopt_array($ch, $defaults);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $params);
        $output = curl_exec($ch);
        if(!$output) {
            $output =  curl_errno($ch);
        }
        curl_close($ch);
        return $output;
    }

    function deleteUser($deviceId)
    {
        $result = $this->db->query("DELETE FROM users WHERE deviceId='$deviceId'");
        return $result;
    }

    function updateUser($deviceId, $loginName, $loginPassword, $server)
    {
        if ($this->db->connect_error) {
            return $this->db->connect_error;
        }
        $result = $this->db->query("SELECT COUNT(*) FROM users WHERE deviceId='$deviceId'");
        $row = $result->fetch_row();
        if ($row[0] > 0) {
            $rst = $this->db->query("UPDATE users SET userName='$loginName',password='$loginPassword',server='$server' "
                . "WHERE deviceId='$deviceId'");
            return $rst;
        } else {
            $rst = $this->db->query("INSERT INTO users(userName,password,server,deviceId) "
                . "VALUES ('$loginName', '$loginPassword', '$server', '$deviceId')");
            return $rst;
        }
    }
}

$possible_get = array("getUnreadCount", "deleteUser");
$possible_post = array("updateUser");
$value = 0;

$api = new TtRssAPI;
if (isset($_GET["action"]) && in_array($_GET["action"], $possible_get) && isset($_GET["deviceId"])) {
    switch ($_GET["action"]) {
        case "getUnreadCount":
            $value = $api->getUnreadCount($_GET["deviceId"]);
            break;
        case "deleteUser":
            $value = $api->deleteUser($_GET["deviceId"]);
            break;
    }
} else if (isset($_POST["action"]) && in_array($_POST["action"], $possible_post)) {
    switch ($_POST["action"]) {
        case "updateUser":
            $value = $api->updateUser($_POST["deviceId"], $_POST["loginName"], $_POST["loginPassword"], $_POST["server"]);
            break;
    }
}
exit($value);
?>