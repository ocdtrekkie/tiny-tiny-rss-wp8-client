<?php
require 'Cipher.php';

class WPNTypesEnum{       
    const Toast = 'wns/toast';
    const Badge = 'wns/badge';
    const Tile  = 'wns/tile';
    const Raw   = 'wns/raw';
}                         

class WPNResponse{
    public $message = '';
    public $error = false;
    public $httpCode = '';
    
    function __construct($message, $httpCode, $error = false){
        $this->message = $message;
        $this->httpCode = $httpCode;
        $this->error = $error;
    }
}

class WPN{            
    private $access_token = '';
    private $sid = 'ms-app://s-1-15-2-4147448716-1590385754-576955360-2871842374-2396221572-2414149291-1231215389';
    private $secret = 'I5ljzjhag7ujlcbIme7eu8xsvshExnyC';
         
    function __construct(){        
    }
    
    private function get_access_token(){
        if($this->access_token != ''){
            return;
        }

        $str = "grant_type=client_credentials&client_id=$this->sid&client_secret=$this->secret&scope=notify.windows.com";
        $url = "https://login.live.com/accesstoken.srf";

        $ch = curl_init($url);
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, 0);
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_HTTPHEADER, array('Content-Type: application/x-www-form-urlencoded'));
        curl_setopt($ch, CURLOPT_POSTFIELDS, "$str");
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
        $output = curl_exec($ch);
        curl_close($ch);                       

        $output = json_decode($output);

        if(isset($output->error)){
            throw new Exception($output->error_description);
        }

        $this->access_token = $output->access_token;
    }

    public function build_tile_xml(){
        return '<?xml version="1.0" encoding="utf-16"?>'.
        '<tile>'.
            '<visual version="3">'.
                //'<binding template="TileSquare71x71IconWithBadge">'.
                 //   '<image id="1" src="ms-appx:///Assets/SquareTile71x71.png" />'.
                //'</binding>'.
                '<binding template="TileSquare150x150IconWithBadge">'.
                    '<image id="1" src="ms-appx:///Assets/Tiles/IconicTileMediumLarge.png" />'.
                '</binding>'.
            '</visual>'.
        '</tile>';
    }

    public function post_tile($uri, $xml_data, $type = WPNTypesEnum::Tile, $tileTag = ''){
        if($this->access_token == ''){
            $this->get_access_token();
        }
    
        $headers = array('Content-Type: text/xml', "Content-Length: " . strlen($xml_data), "X-WNS-Type: $type", "Authorization: Bearer $this->access_token");
        if($tileTag != ''){
            array_push($headers, "X-WNS-Tag: $tileTag");
        }

        $ch = curl_init($uri);
        # Tiles: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868263.aspx
        # http://msdn.microsoft.com/en-us/library/windows/apps/hh465435.aspx
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, 0);
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($ch, CURLOPT_POSTFIELDS, "$xml_data");
        curl_setopt($ch, CURLOPT_VERBOSE, 1);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
        $output = curl_exec($ch);
        $response = curl_getinfo( $ch );
        curl_close($ch);
    
        $code = $response['http_code'];
        if($code == 200){
            return new WPNResponse('Successfully sent message', $code);
        }
        else if($code == 401){
            $this->access_token = '';
            return $this->post_tile($uri, $xml_data, $type, $tileTag);
        }
        else if($code == 410 || $code == 404){
            return new WPNResponse('Expired or invalid URI', $code, true);
        }
        else{
            error_log($code. ' - ' . $uri);
            return new WPNResponse('Unknown error while sending message', $code, true);
        }
    }

    public function build_badge_xml($cnt){
        return '<?xml version="1.0" encoding="utf-16"?>'.
        '<badge version="1" value="'.$cnt.'"/>';
    }

    public function post_badge($uri, $xml_data, $type = WPNTypesEnum::Badge){
        if($this->access_token == ''){
            $this->get_access_token();
        }
    
        $headers = array('Content-Type: text/xml', "Content-Length: " . strlen($xml_data), "X-WNS-Type: $type", "Authorization: Bearer $this->access_token");        
        $ch = curl_init($uri);
        # Tiles: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868263.aspx
        # http://msdn.microsoft.com/en-us/library/windows/apps/hh465435.aspx
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, 0);
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($ch, CURLOPT_POSTFIELDS, "$xml_data");
        curl_setopt($ch, CURLOPT_VERBOSE, 1);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
        $output = curl_exec($ch);
        $response = curl_getinfo( $ch );
        curl_close($ch);
    
        $code = $response['http_code'];
        if($code == 200){
            return new WPNResponse('Successfully sent message', $code);
        }
        else if($code == 401){
            $this->access_token = '';
            return $this->post_tile($uri, $xml_data, $type);
        }
        else if($code == 410 || $code == 404){
            return new WPNResponse('Expired or invalid URI', $code, true);
        }
        else{
            return new WPNResponse('Unknown error while sending message', $code, true);
        }
    }
}

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

    function getUnreadCount($deviceId) {
        $result = $this->db->query("SELECT * FROM users WHERE deviceId='$deviceId'");
        if (!$result) {
            return 0;
        }
        /* fetch object array */
        $row = $result->fetch_object();
        $server = $row->server;
        $userName = $row->userName;
        //$password = $row->password;
        $password = $this->cipher->decrypt($row->password);
        /* free result set */
        $result->close();
        /* login to tt-rss */
        $loginData = json_decode($this->ttrsscurl($server, '{"op":"login","user":"' . $userName . '","password":"' . $password . '"}'), TRUE);
        if ($loginData == null || $loginData['status'] == 1) {
            return 0;
        }
        $sessionId = $loginData['content']['session_id'];
        /* get counters */
        $counters = json_decode($this->ttrsscurl($server, '{"op":"getCounters","sid":"' . $sessionId . '","output_mode":"f"}'), TRUE);
        if ($counters['status'] == 1) {
            $this->ttrsscurl($server, '{"op":"logout","sid":"' . $sessionId . '"}');
            return 0;
        }
        $unreadCount = 0;
        $counterContent = $counters['content'];
        foreach ($counterContent as $counter) {
            if ($counter['id'] == -3) {
                $unreadCount = $counter['counter'];
                break;
            }
        }
        /* logout */
        $this->ttrsscurl($server, '{"op":"logout","sid":"' . $sessionId . '"}');
        return $unreadCount;
    }

    function ttrsscurl($url, $params) {
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
        curl_setopt_array($ch, $defaults);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $params);
        $output = curl_exec($ch);
        if (!$output) {
            $output = curl_errno($ch);
        }
        curl_close($ch);
        return $output;
    }

    function iterate() {
        $result = $this->db->query("SELECT deviceId,channel FROM users WHERE channelInactive=(0)");
        if (!$result) {
           return -1;
        }
        /* fetch object array */
        while ($row = $result->fetch_object()) {
            $deviceId = $row->deviceId;         
            $cnt = $this->getUnreadCount($deviceId);
            // Call WNS
            $wns = new WPN();
            $xml_tile_data = $wns->build_tile_xml();
            error_log('Send tile to '.$deviceId);
            $responseTile = $wns->post_tile($row->channel, $xml_tile_data);
            if($responseTile->error) { 
                error_log($responseTile->message);
                if($responseTile->httpCode == 410 || $responseTile->httpCode == 404) {
                    $this->db->query("UPDATE users SET channelInactive=(1)"
                    . "WHERE deviceId='$deviceId'");
                }
            } else {
                $xml_data = $wns->build_badge_xml($cnt);
                error_log('Send badge to '.$deviceId);
                $response = $wns->post_badge($row->channel, $xml_data);
                  if($response->error) { 
                   error_log($response->message);
                   if($response->httpCode == 410 || $response->httpCode == 404) {
                       $this->db->query("UPDATE users SET channelInactive=(1)"
                       . "WHERE deviceId='$deviceId'");
                   }
                } 
            }
        }
        /* free result set */
        $result->close();
    }
}
$options = getopt("p:");
if ($options["p"] != 'myHash') {
    echo "You are not allowed to run this script.";
    return -1;
} else {
$api = new TtRssAPI;
$api->iterate();

return 1;
}