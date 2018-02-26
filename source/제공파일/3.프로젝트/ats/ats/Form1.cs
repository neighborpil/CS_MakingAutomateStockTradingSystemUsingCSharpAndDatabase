using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading; //스레드 라이브러리 추가

namespace ats
{
    public partial class Form1 : Form //From1 클래스는 Form 클래스를 상속받는다.
    {
        string g_user_id = null; //키움증권 아이디
        string g_accnt_no = null; //증권계좌번호

        public Form1()
        {
            InitializeComponent();

            //OPEN API 이벤트 매서드 정의
            this.axKHOpenAPI1.OnReceiveTrData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEventHandler(this.axKHOpenAPI1_OnReceiveTrData);
            this.axKHOpenAPI1.OnReceiveMsg += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEventHandler(this.axKHOpenAPI1_OnReceiveMsg);
            this.axKHOpenAPI1.OnReceiveChejanData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEventHandler(this.axKHOpenAPI1_OnReceiveChejanData);
        }

        public string get_cur_tm() //현재시간 가져오기 매서드
        {
            DateTime l_cur_time;
            string l_cur_tm;

            l_cur_time = DateTime.Now;
            l_cur_tm = l_cur_time.ToString("HHmmss"); //시분초 가져오기

            return l_cur_tm;
        }

        public string get_jongmok_nm(string i_jongmok_cd) //종목명 가져오기 매서드
        {
            string l_jongmok_nm = null;

            l_jongmok_nm = axKHOpenAPI1.GetMasterCodeName(i_jongmok_cd); //GetMasterCodeName 함수 호출

            return l_jongmok_nm;
        }

        private OracleConnection connect_db() // 오라클 DB접속 매서드
        {
            String conninfo = "User Id=ats; Password=1234; Data Source=(DESCRIPTION =   (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521))   (CONNECT_DATA =     (SERVER = DEDICATED)     (SERVICE_NAME = xe)   ) );";

            OracleConnection conn = new OracleConnection(conninfo);

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("connect_db() FAIL! " + ex.Message, "오류 발생");
                conn = null;
            }
            return conn;
        }

        public void write_msg_log(String text, int is_clear) //로그 메시지 기록 매서드
        {
            DateTime l_cur_time;
            String l_cur_dt;
            String l_cur_tm;
            String l_cur_dtm;

            l_cur_dt = "";
            l_cur_tm = "";

            l_cur_time = DateTime.Now;
            l_cur_dt = l_cur_time.ToString("yyyy-") + l_cur_time.ToString("MM-") + l_cur_time.ToString("dd");
            l_cur_tm = l_cur_time.ToString("HH:mm:ss");

            l_cur_dtm = "[" + l_cur_dt + " " + l_cur_tm + "]";

            if (is_clear == 1)
            {
                if (this.textBox_msg_log.InvokeRequired)
                {
                    textBox1.BeginInvoke(new Action(() => textBox_msg_log.Clear()));
                }
                else
                {
                    this.textBox1.Clear();
                }
            }
            else
            {
                if (this.textBox_msg_log.InvokeRequired)
                {
                    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.AppendText(l_cur_dtm + text)));
                }
                else
                {
                    this.textBox_msg_log.AppendText(l_cur_dtm + text);
                }
            }
        }

        public void write_err_log(String text, int is_clear) //에러 메시지 기록 매서드
        {
            DateTime l_cur_time;
            String l_cur_dt;
            String l_cur_tm;
            String l_cur_dtm;

            l_cur_dt = "";
            l_cur_tm = "";

            l_cur_time = DateTime.Now;
            l_cur_dt = l_cur_time.ToString("yyyy-") + l_cur_time.ToString("MM-") + l_cur_time.ToString("dd");
            l_cur_tm = l_cur_time.ToString("HH:mm:ss");

            l_cur_dtm = "[" + l_cur_dt + " " + l_cur_tm + "]";

            if (is_clear == 1)
            {
                if (this.textBox_err_log.InvokeRequired)
                {
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.Clear()));
                }
                else
                {
                    this.textBox_err_log.Clear();
                }
            }
            else
            {
                if (this.textBox_err_log.InvokeRequired)
                {
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.AppendText(l_cur_dtm + text)));
                }
                else
                {
                    this.textBox_err_log.AppendText(l_cur_dtm + text);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public DateTime delay(int MS) //딜레이 매서드
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                try
                {
                    unsafe
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
                catch (AccessViolationException ex)
                {
                    write_err_log("delay() ex.Message : [" + ex.Message + "]\n", 0);
                }

                ThisMoment = DateTime.Now;
            }
            return DateTime.Now;
        }

        int g_scr_no = 0; //OPEN API 요청번호

        private string get_scr_no() //OPEN API 화면번호 가져오기 매서드
        {
            if (g_scr_no < 9999)
                g_scr_no++;
            else
                g_scr_no = 1000;

            return g_scr_no.ToString();
        }

        private void 로그인ToolStripMenuItem_Click(object sender, EventArgs e) //로그인 매서드
        {
            int ret = 0;
            int ret2 = 0;

            String l_accno = null;
            String l_accno_cnt = null;
            String[] l_accno_arr = null;

            ret = axKHOpenAPI1.CommConnect(); //로그인 요청

            if (ret == 0)
            {
                toolStripStatusLabel1.Text = "로그인중...";

                for (;;)
                {
                    ret2 = axKHOpenAPI1.GetConnectState();
                    if (ret2 == 1)
                    {
                        break;
                    }
                    else
                    {
                        delay(1000);
                    }
                }

                toolStripStatusLabel1.Text = "로그인 완료";

                g_user_id = "";
                g_user_id = axKHOpenAPI1.GetLoginInfo("USER_ID").Trim(); //키움증권 ID 세팅
                textBox1.Text = g_user_id;

                l_accno_cnt = "";
                l_accno_cnt = axKHOpenAPI1.GetLoginInfo("ACCOUNT_CNT").Trim(); //증권계좌번호의 수 가져오기
                l_accno_arr = new String[int.Parse(l_accno_cnt)];

                l_accno = "";
                l_accno = axKHOpenAPI1.GetLoginInfo("ACCNO").Trim();//증권계좌번호 가져오기

                l_accno_arr = l_accno.Split(';');

                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(l_accno_arr); //N개의 증권계좌번호를 콤보박스에 저장
                comboBox1.SelectedIndex = 0;
                g_accnt_no = comboBox1.SelectedItem.ToString().Trim(); //증권계좌번호 세팅

            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) //증권계좌번호 콤보박스 이벤트 매서트
        {
            g_accnt_no = comboBox1.SelectedItem.ToString().Trim();
            write_msg_log("사용할 증권계좌번호는 : [" + g_accnt_no + "] 입니다. \n", 0);
        }

        private void 로그아웃ToolStripMenuItem_Click(object sender, EventArgs e) //로그아웃 매서드
        {
            axKHOpenAPI1.CommTerminate();
            toolStripStatusLabel1.Text = "로그아웃이 완료되었습니다.";
        }

        private void button1_Click(object sender, EventArgs e) //거래종목 테이블 조회
        {
            OracleCommand cmd;
            OracleConnection conn;
            OracleDataReader reader = null;

            string sql;

            string l_jongmok_cd;
            string l_jongmok_nm;
            int l_priority;
            int l_buy_amt;
            int l_buy_price;
            int l_target_price;
            int l_cut_loss_price;
            string l_buy_trd_yn;
            string l_sell_trd_yn;
            int l_seq = 0;


            string[] l_arr = null;

            conn = null;
            conn = connect_db();

            cmd = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = null;
            sql = " SELECT                  " +
                  "    JONGMOK_CD   ,       " +
                  "    JONGMOK_NM   ,       " +
                  "    PRIORITY     ,       " +
                  "    BUY_AMT   ,       " +
                  "    BUY_PRICE   ,       " +
                  "    TARGET_PRICE   ,       " +
                  "    CUT_LOSS_PRICE   ,       " +
                  "    BUY_TRD_YN   ,        " +
                  "    SELL_TRD_YN            " +
                  " FROM                    " +
                  "    TB_TRD_JONGMOK  " +
                  " WHERE USER_ID = " + "'" + g_user_id + "' order by PRIORITY ";

            cmd.CommandText = sql;

            this.Invoke(new MethodInvoker(
            delegate ()
            {
                dataGridView1.Rows.Clear();
            }));

            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                write_err_log("SELECT TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
            }

            l_jongmok_cd = "";
            l_jongmok_nm = "";
            l_priority = 0;
            l_buy_amt = 0;
            l_buy_price = 0;
            l_target_price = 0;
            l_cut_loss_price = 0;
            l_buy_trd_yn = "";
            l_sell_trd_yn = "";
            l_seq = 0;


            while (reader.Read())
            {
                l_seq++;
                l_jongmok_cd = "";
                l_jongmok_nm = "";
                l_priority = 0;
                l_buy_amt = 0;
                l_buy_price = 0;
                l_target_price = 0;
                l_cut_loss_price = 0;
                l_buy_trd_yn = "";
                l_sell_trd_yn = "";

                l_jongmok_cd = reader[0].ToString().Trim();
                l_jongmok_nm = reader[1].ToString().Trim();
                l_priority = int.Parse(reader[2].ToString().Trim());
                l_buy_amt = int.Parse(reader[3].ToString().Trim());
                l_buy_price = int.Parse(reader[4].ToString().Trim());
                l_target_price = int.Parse(reader[5].ToString().Trim());
                l_cut_loss_price = int.Parse(reader[6].ToString().Trim());

                l_buy_trd_yn = reader[7].ToString().Trim();
                l_sell_trd_yn = reader[8].ToString().Trim();

                l_arr = null;
                l_arr = new String[] {
                    l_seq.ToString(),
                    l_jongmok_cd,
                    l_jongmok_nm,
                    l_priority.ToString(),
                    l_buy_amt.ToString(),
                    l_buy_price.ToString(),
                    l_target_price.ToString(),
                    l_cut_loss_price.ToString(),
                    l_buy_trd_yn,
                    l_sell_trd_yn
                };


                this.Invoke(new MethodInvoker(
                delegate ()
                {
                    dataGridView1.Rows.Add(l_arr);
                }));
            }

            write_msg_log("TB_TRD_JONGMOK 테이블이 조회 되었습니다.\n", 0);
        }

        private void button2_Click(object sender, EventArgs e) //거래종목 삽입
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;

            string l_jongmok_cd;
            string l_jongmok_nm;
            int l_priority;
            int l_buy_amt;
            int l_buy_price;
            int l_target_price;
            int l_cut_loss_price;
            string l_buy_trd_yn;
            string l_sell_trd_yn;

            foreach (DataGridViewRow Row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) != true)
                {
                    continue;
                }
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) == true)
                {
                    l_jongmok_cd = Row.Cells[1].Value.ToString();
                    l_jongmok_nm = Row.Cells[2].Value.ToString();
                    l_priority = int.Parse(Row.Cells[3].Value.ToString());
                    l_buy_amt = int.Parse(Row.Cells[4].Value.ToString());
                    l_buy_price = int.Parse(Row.Cells[5].Value.ToString());

                    l_target_price = int.Parse(Row.Cells[6].Value.ToString());
                    l_cut_loss_price = int.Parse(Row.Cells[7].Value.ToString());

                    l_buy_trd_yn = Row.Cells[8].Value.ToString();
                    l_sell_trd_yn = Row.Cells[9].Value.ToString();


                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;

                    sql = @"insert into TB_TRD_JONGMOK values " +
                            "(" +
                                "'" + g_user_id + "'" + "," +
                                "'" + l_jongmok_cd + "'" + "," +
                                "'" + l_jongmok_nm + "'" + "," +
                                    + l_priority + "," +
                                    + l_buy_amt  + "," +
                                    + l_buy_price + "," +
                                    + l_target_price + "," +
                                    + l_cut_loss_price + "," +
                                "'" + l_buy_trd_yn + "'" + "," +
                                "'" + l_sell_trd_yn + "'" + "," +
                                "'" + g_user_id + "'" + "," +
                                "sysdate " + "," +
                                "NULL" + "," +
                                "NULL" +
                            ")";

                    cmd.CommandText = sql;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_err_log("insert TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
                    }

                    write_msg_log("종목코드 : [" + l_jongmok_cd + "]" + "가 삽입 되었습니다.\n", 0);

                    conn.Close();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e) //거래종목 수정
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;

            string l_jongmok_cd;
            string l_jongmok_nm;
            int l_priority;
            int l_buy_amt;
            int l_buy_price;
            int l_target_price;
            int l_cut_loss_price;
            string l_buy_trd_yn;
            string l_sell_trd_yn;

            foreach (DataGridViewRow Row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) != true)
                {
                    continue;
                }
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) == true)
                {
                    l_jongmok_cd = Row.Cells[1].Value.ToString();
                    l_jongmok_nm = Row.Cells[2].Value.ToString();
                    l_priority = int.Parse(Row.Cells[3].Value.ToString());
                    l_buy_amt = int.Parse(Row.Cells[4].Value.ToString());
                    l_buy_price = int.Parse(Row.Cells[5].Value.ToString());
                    l_target_price = int.Parse(Row.Cells[6].Value.ToString()); ;
                    l_cut_loss_price = int.Parse(Row.Cells[7].Value.ToString()); ;
                    l_buy_trd_yn = Row.Cells[8].Value.ToString();
                    l_sell_trd_yn = Row.Cells[9].Value.ToString();


                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;

                    sql = @" UPDATE TB_TRD_JONGMOK
                             SET
	                              JONGMOK_NM = " + "'" + l_jongmok_nm + "'" + "," +
                                " PRIORITY = " + l_priority + "," +
                                " BUY_AMT = " + l_buy_amt + "," +
                                " BUY_PRICE = " + l_buy_price + "," +
                                " TARGET_PRICE = " + l_target_price + "," +
                                " CUT_LOSS_PRICE = " + l_cut_loss_price + "," +
                                " BUY_TRD_YN = " + "'" + l_buy_trd_yn + "'" + "," +
                                " SELL_TRD_YN = " + "'" + l_sell_trd_yn + "'" + "," +
                                " UPDT_ID = " + "'" + g_user_id + "'" + "," +
                                " UPDT_DTM = SYSDATE " +
                            " WHERE JONGMOK_CD = " + "'" + l_jongmok_cd + "'" +
                            " AND USER_ID = " + "'" + g_user_id + "'";

                    cmd.CommandText = sql;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_err_log("UPDATE TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
                    }

                    write_msg_log("종목코드 : [" + l_jongmok_cd + "]" + "가 수정 되었습니다.\n", 0);

                    conn.Close();
                }
            }
        }

        private void button4_Click(object sender, EventArgs e) //거래종목 삭제
        {
            OracleCommand cmd;
            OracleConnection conn;

            string sql;

            string l_jongmok_cd = null;


            foreach (DataGridViewRow Row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) != true)
                {
                    continue;
                }
                if (Convert.ToBoolean(Row.Cells[check.Name].Value) == true)
                {
                    l_jongmok_cd = Row.Cells[1].Value.ToString();

                    conn = null;
                    conn = connect_db();

                    cmd = null;
                    cmd = new OracleCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;

                    sql = null;

                    sql = @" DELETE FROM TB_TRD_JONGMOK " +
                           " WHERE JONGMOK_CD = " + "'" + l_jongmok_cd + "'" +
                           " AND USER_ID = " + "'" + g_user_id + "'";

                    cmd.CommandText = sql;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        write_err_log("DELETE TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
                    }

                    write_msg_log("종목코드 : ["+ l_jongmok_cd+"]"+"가 삭제되었습니다.\n", 0);

                    conn.Close();

                }
            }
        }

        int g_is_thread = 0; //0이면 스레드 미생성, 1이면 스레드 생성
        Thread thread1 = null; //생성된 스레드 객체를 담을 변수

        private void button5_Click(object sender, EventArgs e) //자동매매 시작 매서드
        {
            if (g_is_thread == 1) //스레드가 이미 생성된 상태라면 중복 스레드 생성을 방지함
            {
                write_msg_log("자동매매가 이미 시작되었습니다.\n", 0);
                return; //이벤트 매서드 종료
            }

            g_is_thread = 1; //스레드 생성으로 값 세팅
            thread1 = new Thread(new ThreadStart(m_thread1)); //스레드 생성
            thread1.Start(); //스레드 시작

        }

        public void m_thread1() //스레드 매서드
        {
            string l_cur_tm = null;

            int l_set_tb_accnt_flag = 0; //1이면 호출완료
            int l_set_tb_accnt_info_flag = 0; //1이면 호출완료
            int l_sell_ord_first_flag = 0; //1이면 호출 완료

            if (g_is_thread == 0) //최초 스레드 생성
            {
                g_is_thread = 1; //중복 스레드 생성방지
                write_msg_log("자동매매가 시작되었습니다.\n", 0);
            }

            for (;;) // 첫번째 무한루프의 시작
            {
                l_cur_tm = get_cur_tm();//현재시간을 조회
                if (l_cur_tm.CompareTo("000000") >= 0) // 08시 30분 이후라면 //테스트용시간 : 000000 장운영시간 : 083001
                {
                    //계좌조회, 계좌정보 조회, 보유종목 매도주문 수행
                    if (l_set_tb_accnt_flag == 0) //호출전
                    {
                        write_msg_log("set_tb_accnt() 시작\n", 0);
                        set_tb_accnt(); //호출
                        write_msg_log("set_tb_accnt() 종료\n", 0);

                        l_set_tb_accnt_flag = 1; //호출로 세팅
                    }
                    if (l_set_tb_accnt_info_flag == 0)
                    {
                        write_msg_log("set_tb_accnt_info() 시작\n", 0);
                        set_tb_accnt_info(); //계좌정보 테이블 세팅
                        write_msg_log("set_tb_accnt_info() 종료\n", 0);

                        l_set_tb_accnt_info_flag = 1;
                    }
                    if (l_sell_ord_first_flag == 0)
                    {
                        write_msg_log("sell_ord_first() 시작\n", 0);
                        sell_ord_first(); //보유 종목 매도
                        write_msg_log("sell_ord_first() 종료\n", 0);

                        l_sell_ord_first_flag = 1;
                    }
                }

                if (l_cur_tm.CompareTo("000000") >= 0) // 09시 이후라면 //테스트용시간 : 000000 장운영시간 : 090001
                {
                    for (;;) // 두번째 무한루프의 시작
                    {
                        l_cur_tm = get_cur_tm(); //현재시간을 조회
                        if (l_cur_tm.CompareTo("235959") >= 0) //15시 30분 이후라면 //테스트용시간 : 235959 장운영시간 : 153001
                        {
                            break; //두번째 무한루프를 빠져나감
                        }

                        //장 운영시간중이므로 매수 혹은 매도주문

                        write_msg_log("real_buy_ord() 시작\n", 0);
                        real_buy_ord(); //실시간 매수 주문 매서드 호출
                        write_msg_log("real_buy_ord() 종료\n", 0);

                        delay(200); //0.2초 딜레이

                        write_msg_log("real_sell_ord() 시작\n", 0);
                        real_sell_ord(); //실시간 매도 주문 매서드 호출
                        write_msg_log("real_sell_ord() 종료\n", 0);

                        delay(200); //0.2초 딜레이

                        write_msg_log("real_cut_loss_ord() 시작\n", 0);
                        real_cut_loss_ord(); //실시간 손절 주문 매서드 호출
                        write_msg_log("real_cut_loss_ord() 종료\n", 0);

                        delay(200); //0.2초 딜레이
                    }
                }
                delay(200); //첫번째 무한루프 Delay
            }
        }

        public void update_tb_trd_jongmok(String i_jongmok_cd)
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;


            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;
        
            l_sql = @" update TB_TRD_JONGMOK set buy_trd_yn = 'N', updt_dtm = SYSDATE, updt_id = 'ats' " +
            " where user_id = " + "'" + g_user_id + "'" +
            " and jongmok_cd = " + "'" + i_jongmok_cd + "'";

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("update TB_TRD_JONGMOK ex.Message : [" + ex.Message + "]\n", 0);
            }

            conn.Close();
        }

        private void button6_Click(object sender, EventArgs e) //자동매매 중지 매서드
        {
            write_msg_log("\n자동매매중지 시작\n", 0);

            try
            {
                thread1.Abort(); //스레드 중지
            }
            catch (Exception ex)
            {
                write_err_log("자동매매중지 ex.Message : " + ex.Message + "\n", 0);
            }

            this.Invoke(new MethodInvoker(() =>
            {
                if (thread1 != null)
                {
                    thread1.Interrupt();
                    thread1 = null;
                }
            }));

            g_is_thread = 0; //스레드 미실행으로 세팅

            write_msg_log("\n자동매매중지 완료\n", 0);
        }

        int g_buy_hoga = 0; //최우선매수호가 저장 변수
        int g_flag_7 = 0; //최우선매수호가 플래스 변수 1이면 조회 완료


        public void real_buy_ord() //실시간 매수 주문 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            string l_jongmok_cd = null;
            int l_buy_amt = 0;
            int l_buy_price = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //거래종목 테이블 조회
            sql = @"                    " +
                   " SELECT             " +
                   "     A.JONGMOK_CD,  " +
                   "     A.BUY_AMT,     " +
                   "     A.BUY_PRICE    " +
                   " FROM TB_TRD_JONGMOK A " +
                   " WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                   " AND A.BUY_TRD_YN = 'Y' " +
                   " ORDER BY A.PRIORITY ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                l_jongmok_cd = "";
                l_buy_amt = 0;
                l_buy_price = 0;

                l_jongmok_cd = reader[0].ToString().Trim(); //종목코드
                l_buy_amt = int.Parse(reader[1].ToString().Trim()); //매수금액
                l_buy_price = int.Parse(reader[2].ToString().Trim()); //매수가격

                int l_buy_price_tmp = 0;
                l_buy_price_tmp = get_hoga_unit_price(l_buy_price, l_jongmok_cd, 0); //매수호가 구하기

                int l_buy_ord_stock_cnt = 0;
                l_buy_ord_stock_cnt = (int)(l_buy_amt / l_buy_price_tmp); //매수주문주식수 구하기

                write_msg_log("종목코드 : [" + l_jongmok_cd.ToString() + "]\n", 0);
                write_msg_log("종목명 : [" + get_jongmok_nm(l_jongmok_cd) + "]\n", 0);
                write_msg_log("매수금액 : [" + l_buy_amt.ToString() + "]\n", 0);
                write_msg_log("매수가격 : [" + l_buy_price_tmp.ToString() + "]\n", 0);

                int l_own_stock_cnt = 0;
                l_own_stock_cnt = get_own_stock_cnt(l_jongmok_cd); //해당 종목에 대한 보유주식수 구하기
                write_msg_log("보유주식수 : [" + l_own_stock_cnt.ToString() + "]\n", 0);

                if (l_own_stock_cnt > 0) //해당 종목이 보유중이라면 매수하지 않음
                {
                    write_msg_log("해당 종목이 보유중이므로 매수하지 않음\n", 0);
                    continue;
                }

                string l_buy_not_chegyul_yn = null;
                l_buy_not_chegyul_yn = get_buy_not_chegyul_yn(l_jongmok_cd); //매수미체결주문이 있는지 확인

                if (l_buy_not_chegyul_yn == "Y") //매수미체결주문이 있으므로 매수하지 않음
                {
                    write_msg_log("해당 종목이 매수 미체결 주문이 매수하지 않음 \n", 0);
                    continue;
                }

                int l_for_flag = 0;
                int l_for_cnt = 0; 
                l_for_flag = 0;
                g_buy_hoga = 0;
                for (;;) //20160609 이경오 수정 
                {
                    g_rqname = "";
                    g_rqname = "호가조회";
                    g_flag_7 = 0;
                    axKHOpenAPI1.SetInputValue("종목코드", l_jongmok_cd);

                    string l_scr_no_2 = null;
                    l_scr_no_2 = "";
                    l_scr_no_2 = get_scr_no();

                    axKHOpenAPI1.CommRqData("호가조회", "opt10004", 0, l_scr_no_2);

                    try
                    {
                        l_for_cnt = 0;
                        for (;;)
                        {
                            if (g_flag_7 == 1)
                            {
                                delay(200);
                                axKHOpenAPI1.DisconnectRealData(l_scr_no_2);
                                l_for_flag = 1;
                                break;
                            }
                            else
                            {
                                write_msg_log("'호가조회' 완료 대기중...\n", 0);
                                delay(200);
                                l_for_cnt++;
                                if (l_for_cnt == 5)
                                {
                                    l_for_flag = 0;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        write_err_log("real_buy_ord() 호가조회 ex.Message : [" + ex.Message + "]\n", 0);
                    }

                    axKHOpenAPI1.DisconnectRealData(l_scr_no_2);

                    if (l_for_flag == 1)
                    {
                        break;
                    }
                    else if (l_for_flag == 0)
                    {
                        delay(200);
                        continue;
                    }
                    delay(200);
                }

                if(l_buy_price > g_buy_hoga)
                {
                    write_msg_log("해당 종목의 매수가격이 최우선 매수호가 보다 크므로 매수주문 하지 않음 \n", 0);
                    continue;
                }


                g_flag_3 = 0;
                g_rqname = "매수주문";

                String l_scr_no = null;
                l_scr_no = "";
                l_scr_no = get_scr_no();

                int ret = 0;

                //매수주문 요청 
                ret = axKHOpenAPI1.SendOrder("매수주문", l_scr_no, g_accnt_no,
                                                1, l_jongmok_cd, l_buy_ord_stock_cnt,
                                                l_buy_price, "00", "");
                if (ret == 0)
                {
                    write_msg_log("매수주문 SendOrder() 호출 성공\n", 0);
                    write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                }
                else
                {
                    write_msg_log("매수주문 SendOrder() 호출 실패\n", 0);
                    write_msg_log("i_jongmok_cd : [" + l_jongmok_cd + "]\n", 0);
                }

                delay(200); //0.2초 딜레이 

                for (;;)
                {
                    if (g_flag_3 == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(l_scr_no);
                        break;
                    }
                    else
                    {
                        write_msg_log("'매수주문' 완료 대기중...\n", 0);
                        delay(200);
                        break;
                    }
                }
                axKHOpenAPI1.DisconnectRealData(l_scr_no);
                delay(1000); //1초 딜레이

            } // while (reader.Read()) 종료

            reader.Close();
            conn.Close();
        }

        public int get_own_stock_cnt(string i_jongmok_cd) //보유주식수 가져오기 매서드 
        {

            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            int l_own_stock_cnt = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //계좌정보 테이블 조회 
            sql = @"
                    SELECT
	                    NVL(MAX(OWN_STOCK_CNT), 0) OWN_STOCK_CNT
                    FROM
                    TB_ACCNT_INFO
                    WHERE USER_ID = " + "'" + g_user_id + "'" +
                    " AND JONGMOK_CD = " + "'" + i_jongmok_cd + "'" +
                    " AND ACCNT_NO = " + "'" + g_accnt_no + "'" +
                    " AND REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') ";

            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            reader.Read();

            l_own_stock_cnt = int.Parse(reader[0].ToString()); //보유주식수 구하기 

            reader.Close();
            conn.Close();

            return l_own_stock_cnt;
        }

        public string get_buy_not_chegyul_yn(string i_jongmok_cd) //매수 미체결 여부 가져오기 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            int l_buy_not_chegyul_ord_stock_cnt = 0;
            string l_buy_not_chegyul_yn = null;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //주문내역 및 체결내역 테이블 조회 
            sql = @"
            select
                nvl(sum(ord_stock_cnt - CHEGYUL_STOCK_CNT), 0) buy_not_chegyul_ord_stock_cnt
            from
            (
                select ord_stock_cnt ord_stock_cnt,
                ( select nvl(max(b.CHEGYUL_STOCK_CNT), 0) CHEGYUL_STOCK_CNT
                      from tb_chegyul_lst b
                      where b.user_id = a.user_id
                      and b.accnt_no = a.accnt_no
                      and b.ref_dt = a.ref_dt
                      and b.jongmok_cd = a.jongmok_cd
                      and b.ord_gb = a.ord_gb
                      and b.ord_no = a.ord_no
                    ) CHEGYUL_STOCK_CNT
                from
                TB_ORD_LST a
                where a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                and a.user_id = " + "'" + g_user_id + "'" +
                        " and a.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                        " and a.jongmok_cd = " + "'" + i_jongmok_cd + "'" +
                        " and a.ord_gb = '2' " +
                        " and a.org_ord_no = '0000000' " +
                        " and not exists ( select '1' " +
                " 		            from TB_ORD_LST b " +
                " 		            where b.user_id = a.user_id " +
                " 		            and b.accnt_no = a.accnt_no " +
                " 		            and b.ref_dt = a.ref_dt " +
                " 		            and b.jongmok_cd = a.jongmok_cd " +
                " 		            and b.ord_gb = a.ord_gb " +
                " 		            and b.org_ord_no = a.ord_no " +
                " 	            ) " +
                    " ) x ";


            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            reader.Read();

            l_buy_not_chegyul_ord_stock_cnt = int.Parse(reader[0].ToString()); //매수 미체결 주식수 구하기 

            reader.Close();
            conn.Close();

            if (l_buy_not_chegyul_ord_stock_cnt > 0)
            {
                l_buy_not_chegyul_yn = "Y";
            }
            else
            {
                l_buy_not_chegyul_yn = "N";
            }

            return l_buy_not_chegyul_yn;
        }

        public void real_sell_ord() //실시간 매도 주문 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            string l_jongmok_cd = null;
            int l_target_price = 0;
            int l_own_stock_cnt = 0;

            write_msg_log("real_sell_ord 시작\n", 0);
            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //거래종목 및 계좌정보 테이블 조회 
            sql = @" SELECT " +
                   "     A.JONGMOK_CD, " +
                   "     A.TARGET_PRICE, " +
                   "     B.OWN_STOCK_CNT " +
                   " FROM " +
                   "     TB_TRD_JONGMOK A, " +
                   "     TB_ACCNT_INFO B " +
                   " WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                   " AND A.JONGMOK_CD = B.JONGMOK_CD " +
                   " AND B.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                   " AND B.REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') " +
                   " AND A.SELL_TRD_YN = 'Y' AND B.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                l_jongmok_cd = "";
                l_target_price = 0;

                l_jongmok_cd = reader[0].ToString().Trim();
                l_target_price = int.Parse(reader[1].ToString().Trim());
                l_own_stock_cnt = int.Parse(reader[2].ToString().Trim());

                write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                write_msg_log("종목명 : [" + get_jongmok_nm(l_jongmok_cd) + "]\n", 0);
                write_msg_log("목표가격 : [" + l_target_price.ToString() + "]\n", 0);
                write_msg_log("보유주식수 : [" + l_own_stock_cnt.ToString() + "]\n", 0);

                int l_sell_not_chegyul_ord_stock_cnt = 0;
                l_sell_not_chegyul_ord_stock_cnt = get_sell_not_chegyul_ord_stock_cnt(l_jongmok_cd); //매도 미체결 주식수 구하기

                if (l_sell_not_chegyul_ord_stock_cnt == l_own_stock_cnt) //매도미체결주식수와 보유주식수가 같다면 기 주문종목이므로 매도주문하지 않음 
                {
                    continue;
                }
                else //매도미체결주식수와 보유주식수가 같지 않다면 아직 매도하지 않은 종목임 
                {
                    int l_sell_ord_stock_cnt_tmp = 0;
                    l_sell_ord_stock_cnt_tmp = l_own_stock_cnt - l_sell_not_chegyul_ord_stock_cnt; //보유주식수에서 매도미체결주식수를 빼서 매도주문주식수를 구함

                    if(l_sell_ord_stock_cnt_tmp <= 0) //매도대상주식수가 0이하라면 매도하지 않음
                    {
                        continue; 
                    }

                    int l_new_target_price = 0;
                    l_new_target_price = get_hoga_unit_price(l_target_price, l_jongmok_cd, 0); //매도호가를 구함 

                    g_flag_4 = 0;
                    g_rqname = "매도주문";

                    String l_scr_no = null;
                    l_scr_no = "";
                    l_scr_no = get_scr_no();


                    int ret = 0;

                    //매도주문 요청 
                    ret = axKHOpenAPI1.SendOrder("매도주문", l_scr_no, g_accnt_no,
                                                    2, l_jongmok_cd, l_sell_ord_stock_cnt_tmp,
                                                    l_new_target_price, "00", "");

                    if (ret == 0)
                    {
                        write_msg_log("매도주문 Sendord() 호출 성공\n", 0);
                        write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                    }
                    else
                    {
                        write_msg_log("매도주문 Sendord() 호출 실패\n", 0);
                        write_msg_log("i_jongmok_cd : [" + l_jongmok_cd + "]\n", 0);
                    }

                    delay(200); //0.2초 딜레이 

                    for (;;)
                    {
                        if (g_flag_4 == 1)
                        {
                            delay(200);
                            axKHOpenAPI1.DisconnectRealData(l_scr_no);
                            break;
                        }
                        else
                        {
                            write_msg_log("'매도주문' 완료 대기중...\n", 0);
                            delay(200);
                            break;
                        }
                    }
                    axKHOpenAPI1.DisconnectRealData(l_scr_no);
                }
            } //while (reader.Read()) 종료

            reader.Close();
            conn.Close();
        }

        public int get_sell_not_chegyul_ord_stock_cnt(string i_jongmok_cd) //매도미체결주식수 가져오기 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            int l_sell_not_chegyul_ord_stock_cnt = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //주문내역 및 체결내역 테이블 조회 
            sql = @"
                    select
                        nvl(sum(ord_stock_cnt - CHEGYUL_STOCK_CNT), 0) sell_not_chegyul_ord_stock_cnt
                    from
                    (
                    select
                        ord_stock_cnt ord_stock_cnt,
                        ( select nvl(max(b.CHEGYUL_STOCK_CNT), 0) CHEGYUL_STOCK_CNT
                        from tb_chegyul_lst b
                        where b.user_id = a.user_id
                        and b.accnt_no = a.accnt_no
                        and b.ref_dt = a.ref_dt
                        and b.jongmok_cd = a.jongmok_cd
                        and b.ord_gb = a.ord_gb
                        and b.ord_no = a.ord_no
                        ) CHEGYUL_STOCK_CNT
                    from TB_ORD_LST a
                    where a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                    and a.user_id = " + "'" + g_user_id + "'" +
                    " and a.jongmok_cd = " + "'" + i_jongmok_cd + "'" +
                    " and a.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                    " and a.ord_gb = '1' " +
                    " and a.org_ord_no = '0000000' " +
                    " and not exists ( select '1' " +
                    "                 from TB_ORD_LST b " +
                    "                 where b.user_id = a.user_id " +
                    "                 and b.accnt_no = a.accnt_no " +
                    "                 and b.ref_dt = a.ref_dt " +
                    "                 and b.jongmok_cd = a.jongmok_cd " +
                    "                 and b.ord_gb = a.ord_gb " +
                    "                 and b.org_ord_no = a.ord_no " +
                    "                )) ";


            cmd.CommandText = sql;

            reader = cmd.ExecuteReader();
            reader.Read();

            l_sell_not_chegyul_ord_stock_cnt = int.Parse(reader[0].ToString()); //매도미체결주식수 가져오기 

            reader.Close();
            conn.Close();

            return l_sell_not_chegyul_ord_stock_cnt;
        }

        int g_cur_price = 0;
        int g_flag_6 = 0;

        public void real_cut_loss_ord() //실시간 손절주문 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            string l_jongmok_cd = null;
            int l_cut_loss_price = 0;
            int l_own_stock_cnt = 0;

            int l_for_flag = 0;
            int l_for_cnt = 0;

            write_msg_log("real_cut_loss_ord 시작\n", 0);

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //거래종목 및 계좌 정보 테이블 조회 
            sql = @" SELECT " +
                   "     A.JONGMOK_CD, " +
                   "     A.CUT_LOSS_PRICE, " +
                   "     B.OWN_STOCK_CNT " +
                   " FROM " +
                   "     TB_TRD_JONGMOK A, " +
                   "     TB_ACCNT_INFO B " +
                   " WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                   " AND A.JONGMOK_CD = B.JONGMOK_CD " +
                   " AND B.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                   " AND B.REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') " +
                   " AND A.SELL_TRD_YN = 'Y' AND B.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                l_jongmok_cd = "";
                l_cut_loss_price = 0;


                l_jongmok_cd = reader[0].ToString().Trim();
                l_cut_loss_price = int.Parse(reader[1].ToString().Trim());
                l_own_stock_cnt = int.Parse(reader[2].ToString().Trim());

                write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                write_msg_log("종목명 : [" + get_jongmok_nm(l_jongmok_cd) + "]\n", 0);
                write_msg_log("손절가격 : [" + l_cut_loss_price.ToString() + "]\n", 0);
                write_msg_log("보유주식수 : [" + l_own_stock_cnt.ToString() + "]\n", 0);


                l_for_flag = 0;
                g_cur_price = 0;

                for (;;) 
                {
                    g_rqname = "";
                    g_rqname = "현재가조회";
                    g_flag_6 = 0;
                    axKHOpenAPI1.SetInputValue("종목코드", l_jongmok_cd);

                    string l_scr_no = null;
                    l_scr_no = "";
                    l_scr_no = get_scr_no();

                    //현재가 조회 요청 
                    axKHOpenAPI1.CommRqData(g_rqname, "opt10001", 0, l_scr_no);

                    try
                    {
                        l_for_cnt = 0;
                        for (;;)
                        {
                            if (g_flag_6 == 1)
                            {
                                delay(200);
                                axKHOpenAPI1.DisconnectRealData(l_scr_no);
                                l_for_flag = 1;
                                break;
                            }
                            else
                            {
                                write_msg_log("'현재가조회' 완료 대기중...\n", 0);
                                delay(200);
                                l_for_cnt++;
                                if (l_for_cnt == 5)
                                {
                                    l_for_flag = 0;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        write_err_log("real_cut_loss_ord() 현재가조회 ex.Message : [" + ex.Message + "]\n", 0);
                    }

                    axKHOpenAPI1.DisconnectRealData(l_scr_no);

                    if (l_for_flag == 1)
                    {
                        break;
                    }
                    else if (l_for_flag == 0)
                    {
                        delay(200);
                        continue;
                    }
                    delay(200);
                } //현재가 조회 완료 

                write_msg_log("현재가 : [" + g_cur_price.ToString() + "]\n", 0);

                if (g_cur_price < l_cut_loss_price) //현재가격이 손절가격을 이탈시 
                {

                    write_msg_log("현재가격이 손절가격을 이탈\n", 0);

                    write_msg_log("sell_canc_ord() 시작\n", 0);
                    sell_canc_ord(l_jongmok_cd);
                    write_msg_log("sell_canc_ord() 완료\n", 0);

                    g_flag_4 = 0;
                    g_rqname = "매도주문";

                    String l_scr_no = null;
                    l_scr_no = "";
                    l_scr_no = get_scr_no();


                    int ret = 0;

                    //매도주문 요청 
                    ret = axKHOpenAPI1.SendOrder("매도주문", l_scr_no, g_accnt_no,
                                                    2, l_jongmok_cd, l_own_stock_cnt,
                                                    0, "03", "");

                    if (ret == 0)
                    {
                        write_msg_log("매도주문 Sendord() 호출 성공\n", 0);
                        write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                    }
                    else
                    {
                        write_msg_log("매도주문 Sendord() 호출 실패\n", 0);
                        write_msg_log("i_jongmok_cd : [" + l_jongmok_cd + "]\n", 0);
                    }

                    delay(200);

                    for (;;)
                    {
                        if (g_flag_4 == 1)
                        {
                            delay(200);
                            axKHOpenAPI1.DisconnectRealData(l_scr_no);
                            break;
                        }
                        else
                        {
                            write_msg_log("'매도주문' 완료 대기중...\n", 0);
                            delay(200);
                            break;
                        }
                    }
                    axKHOpenAPI1.DisconnectRealData(l_scr_no);

                    update_tb_trd_jongmok(l_jongmok_cd);

                }
            } //while (reader.Read()) 종료

            reader.Close();
            conn.Close();
        }

        public void sell_canc_ord(string i_jongmok_cd) //매도 취소 주문 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            string l_rid = null;
            string l_jongmok_cd = null;
            int l_ord_stock_cnt = 0;
            int l_ord_price = 0;
            string l_ord_no = null;
            string l_org_ord_no = null;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;


            //주문내역 및 체결내역 테이블 조회 
            sql = @"select
	                    rowid rid,
	                    jongmok_cd,
	                    (ord_stock_cnt -
	                    ( select nvl(max(b.CHEGYUL_STOCK_CNT), 0) CHEGYUL_STOCK_CNT
	                      from tb_chegyul_lst b
	                      where b.user_id = a.user_id
	                      and b.accnt_no = a.accnt_no
	                      and b.ref_dt = a.ref_dt
	                      and b.jongmok_cd = a.jongmok_cd
	                      and b.ord_gb = a.ord_gb
	                      and b.ord_no = a.ord_no
                      )) sell_not_chegyul_ord_stock_cnt,
	                    ord_price,
	                    ord_no,
	                    org_ord_no
                    from
                    TB_ORD_LST a
                    where a.ref_dt = TO_CHAR(SYSDATE, 'YYYYMMDD')
                    and a.user_id =     " + "'" + g_user_id + "'" +
                    " and a.accnt_no =     " + "'" + g_accnt_no + "'" +
                    " and a.jongmok_cd =     " + "'" + i_jongmok_cd + "'" +
                    " and a.ord_gb = '1' " +
                    " and a.org_ord_no = '0000000' " +
                    " and not exists ( 	select '1' " +
                    " 				    from TB_ORD_LST b " +
                    " 				    where b.user_id = a.user_id " +
                    " 				    and b.accnt_no = a.accnt_no " +
                    " 				    and b.ref_dt = a.ref_dt " +
                    " 				    and b.jongmok_cd = a.jongmok_cd " +
                    " 				    and b.ord_gb = a.ord_gb " +
                    " 				    and b.org_ord_no = a.ord_no " +
                     "                )";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                l_rid = "";
                l_jongmok_cd = "";
                l_ord_stock_cnt = 0;
                l_ord_price = 0;
                l_ord_no = "";
                l_org_ord_no = "";

                l_rid = reader[0].ToString().Trim();
                l_jongmok_cd = reader[1].ToString().Trim();
                l_ord_stock_cnt = int.Parse(reader[2].ToString().Trim());
                l_ord_price = int.Parse(reader[3].ToString().Trim());
                l_ord_no = reader[4].ToString().Trim();
                l_org_ord_no = reader[5].ToString().Trim();
                //////////////////////////////////////////////////////////////////////////////

                g_flag_5 = 0;
                g_rqname = "매도취소주문";

                String l_scr_no = null;
                l_scr_no = "";
                l_scr_no = get_scr_no();


                int ret = 0;

                //매도취소 주문 요청 
                ret = axKHOpenAPI1.SendOrder("매도취소주문", l_scr_no, g_accnt_no,
                                                4, l_jongmok_cd, l_ord_stock_cnt,
                                                0, "03", l_ord_no);

                if (ret == 0)
                {
                    write_msg_log("매도취소주문 Sendord() 호출 성공\n", 0);
                    write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                }
                else
                {
                    write_msg_log("매도취소주문 Sendord() 호출 실패\n", 0);
                    write_msg_log("i_jongmok_cd : [" + l_jongmok_cd + "]\n", 0);
                }

                delay(200);

                for (;;)
                {
                    if (g_flag_5 == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(l_scr_no);
                        break;
                    }
                    else
                    {
                        write_msg_log("매도취소주문 완료 대기중...\n", 0);
                        delay(200);
                        break;
                    }
                }
                axKHOpenAPI1.DisconnectRealData(l_scr_no);

                delay(1000);
                
            }

            reader.Close();
            conn.Close();
        }

        private void axKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e) //OPEN API 요청에 대한 응답 이벤트 매서드 
        {
            if (g_rqname.CompareTo(e.sRQName) == 0) //요청한 요청명과 OPEN API로부터 응답받은 요청명이 같다면
            {
                ; //다음으로 진행
            }
            else //요청한 요청명과 OPEN API로부터 응답받은 요청명이 같지 않다면
            {
                write_err_log("요청한 TR  : [" + g_rqname + "]\n", 0);
                write_err_log("응답받은 TR : [" + e.sRQName + "]\n", 0);

                switch (g_rqname)
                {
                    case "증거금세부내역조회요청":
                        g_flag_1 = 1; //g_flag_1을 1로 세팅하여 요청하는쪽에서 무한루프에 빠지지 않도록 방지함
                        break;
                    case "계좌평가현황요청":
                        g_flag_2 = 1; //g_flag_2을 1로 세팅하여 요청하는쪽에서 무한루프에 빠지지 않도록 방지함
                        break;
                    case "호가조회":
                        g_flag_7 = 1; 
                        break;
                    case "현재가조회":
                        g_flag_6 = 1; //g_flag_6을 1로 세팅하여 요청하는쪽에서 무한루프에 빠지지 않도록 방지함
                        break;
                    default: break;
                }
                return;
            }

            if (e.sRQName == "증거금세부내역조회요청") //응답받은 요청명이 증거금세부내역조회요청이라면 
            {
                g_ord_amt_possible = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, 0, "100주문가능금액").Trim()); //주문가능금액을 저장 
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);
                g_flag_1 = 1;
            }
            if (e.sRQName == "계좌평가현황요청") //응답받은 요청명이 계좌평가현황요청이라면
            {
                int repeat_cnt = 0;
                int ii = 0;

                String user_id = null;
                String jongmok_cd = null;
                String jongmok_nm = null;

                int own_stock_cnt = 0;
                int buy_price = 0;
                int own_amt = 0;

                repeat_cnt = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName); //보유 종목수 가져오기 
                
                write_msg_log("TB_ACCNT_INFO 테이블 세팅 시작\n", 0);

                write_msg_log("보유 종목수 : " + repeat_cnt.ToString() + "\n", 0);

                for (ii = 0; ii < repeat_cnt; ii++)
                {
                    user_id = "";
                    jongmok_cd = "";
                    own_stock_cnt = 0;
                    buy_price = 0;
                    own_amt = 0;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "종목코드").Trim().Substring(1, 6);
                    jongmok_nm = axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "종목명").Trim();
                    own_stock_cnt = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "보유수량").Trim());
                    buy_price = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "평균단가").Trim());
                    own_amt = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "매입금액").Trim());

                    write_msg_log("종목코드 : " + jongmok_cd + "\n", 0);
                    write_msg_log("종목명 : " + jongmok_nm + "\n", 0);
                    write_msg_log("보유주식수 : " + own_stock_cnt.ToString() + "\n", 0);

                    if (own_stock_cnt == 0) //보유주식수가 0이라면 저장하지 않음 
                    {
                        continue;
                    }

                    insert_tb_accnt_info(jongmok_cd, jongmok_nm, buy_price, own_stock_cnt, own_amt); //계좌정보 테이블에 저장 
                }

                write_msg_log("TB_ACCNT_INFO 테이블 세팅 완료\n", 0);

                axKHOpenAPI1.DisconnectRealData(e.sScrNo);

                if (e.sPrevNext.Length == 0)
                {
                    g_is_next = 0;
                }
                else
                {
                    g_is_next = int.Parse(e.sPrevNext);
                }
                g_flag_2 = 1;
            }

            if (e.sRQName == "호가조회")
            {
                int cnt = 0;
                int ii = 0;
                int l_buy_hoga = 0;

                cnt = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);

                for (ii = 0; ii < cnt; ii++)
                {

                    l_buy_hoga = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "매수최우선호가").Trim());
                    l_buy_hoga = System.Math.Abs(l_buy_hoga);                    
                }

                g_buy_hoga = l_buy_hoga;

                axKHOpenAPI1.DisconnectRealData(e.sScrNo);
                g_flag_7 = 1;
            }


            if (e.sRQName == "현재가조회") //응답받은 요청명이 현재가조회라면
            {
                int repeat_cnt;
                int ii;

                repeat_cnt = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);

                for (ii = 0; ii < repeat_cnt; ii++)
                {
                    g_cur_price = int.Parse(axKHOpenAPI1.CommGetData(e.sTrCode, "", e.sRQName, ii, "현재가").Trim()); //현재가 가져오기 
                    g_cur_price = System.Math.Abs(g_cur_price);
                }
                axKHOpenAPI1.DisconnectRealData(e.sScrNo);

                g_flag_6 = 1;
            }
        } //axKHOpenAPI1_OnReceiveTrData 매서드 종료

        int g_flag_3 = 0; //매수주문 응답 플래그 
        int g_flag_4 = 0; //매도주문 응답 플래그 
        int g_flag_5 = 0; //매도취소주문 응답 플래그 

        private void axKHOpenAPI1_OnReceiveMsg(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEvent e) //OPEN API 주문응답 이벤트 매서드 
        {
            if (e.sRQName == "매수주문")
            {
                write_msg_log("\n================매수 주문 원장 응답 정보 출력 시작=================\n", 0);
                write_msg_log("sScrNo : [" + e.sScrNo + "]" + "\n", 0);
                write_msg_log("sRQName : [" + e.sRQName + "]" + "\n", 0);
                write_msg_log("sTrCode : [" + e.sTrCode + "]" + "\n", 0);
                write_msg_log("sMsg : [" + e.sMsg + "]" + "\n", 0);
                write_msg_log("=================매수 주문 원장 응답 정보 출력 종료=================\n", 0);
                g_flag_3 = 1; //매수주문 응답완료 세팅 

            }
            if (e.sRQName == "매도주문")
            {
                write_msg_log("\n================매도 주문 원장 응답 정보 출력 시작=================\n", 0);
                write_msg_log("sScrNo : [" + e.sScrNo + "]" + "\n", 0);
                write_msg_log("sRQName : [" + e.sRQName + "]" + "\n", 0);
                write_msg_log("sTrCode : [" + e.sTrCode + "]" + "\n", 0);
                write_msg_log("sMsg : [" + e.sMsg + "]" + "\n", 0);
                write_msg_log("=================매도 주문 원장 응답 정보 출력 종료=================\n", 0);
                g_flag_4 = 1; //매도주문 응답완료 세팅 
            }
            if (e.sRQName == "매도취소주문")
            {
                write_msg_log("\n================매도취소 주문 원장 응답 정보 출력 시작=================\n", 0);
                write_msg_log("sScrNo : [" + e.sScrNo + "]" + "\n", 0);
                write_msg_log("sRQName : [" + e.sRQName + "]" + "\n", 0);
                write_msg_log("sTrCode : [" + e.sTrCode + "]" + "\n", 0);
                write_msg_log("sMsg : [" + e.sMsg + "]" + "\n", 0);
                write_msg_log("=================매도취소 주문 원장 응답 정보 출력 종료=================\n", 0);
                g_flag_5 = 1; //매도취소주문 응답완료 세팅 
            }
        }

        private void axKHOpenAPI1_OnReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e) //OPEN API 주문내역 및 체결내역 응답 이벤트 매서드 
        {
            if (e.sGubun == "0") //sGubun의 값이 "0" 이라면 주문내역 및 체결내역
            {
                String chejan_gb = "";
                chejan_gb = axKHOpenAPI1.GetChejanData(913).Trim(); //주문내역인지 체결내역인지 가져옴 

                if (chejan_gb == "접수") //chejan_gb 의 값이 "접수"라면 주문내역 
                {
                    String user_id = null;
                    String jongmok_cd = null;
                    String jongmok_nm = null;
                    String ord_gb = null;
                    String ord_no = null;
                    String org_ord_no = null;
                    string ref_dt = null;
                    int ord_price = 0;
                    int ord_stock_cnt = 0;
                    int ord_amt = 0;
                    String ord_dtm = null;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                    jongmok_nm = get_jongmok_nm(jongmok_cd);
                    ord_gb = axKHOpenAPI1.GetChejanData(907).Trim();
                    ord_no = axKHOpenAPI1.GetChejanData(9203).Trim();
                    org_ord_no = axKHOpenAPI1.GetChejanData(904).Trim();
                    ord_price = int.Parse(axKHOpenAPI1.GetChejanData(901).Trim());
                    ord_stock_cnt = int.Parse(axKHOpenAPI1.GetChejanData(900).Trim());
                    ord_amt = ord_price * ord_stock_cnt;

                    DateTime CurTime;
                    String CurDt;
                    CurTime = DateTime.Now;
                    CurDt = CurTime.ToString("yyyy") + CurTime.ToString("MM") + CurTime.ToString("dd");

                    ref_dt = CurDt;

                    ord_dtm = CurDt + axKHOpenAPI1.GetChejanData(908).Trim();

                    write_msg_log("종목코드 : [" + jongmok_cd + "]" + "\n", 0);
                    write_msg_log("종목명 : [" + jongmok_nm + "]" + "\n", 0);
                    write_msg_log("주문구분 : [" + ord_gb + "]" + "\n", 0);
                    write_msg_log("주문번호 : [" + ord_no + "]" + "\n", 0);
                    write_msg_log("원주문번호 : [" + org_ord_no + "]" + "\n", 0);
                    write_msg_log("주문금액 : [" + ord_price.ToString() + "]" + "\n", 0);
                    write_msg_log("주문주식수 : [" + ord_stock_cnt.ToString() + "]" + "\n", 0);
                    write_msg_log("주문금액 : [" + ord_amt.ToString() + "]" + "\n", 0);
                    write_msg_log("주문일시 : [" + ord_dtm + "]" + "\n", 0);

                    insert_tb_ord_lst(ref_dt, jongmok_cd, jongmok_nm, ord_gb, ord_no, org_ord_no, ord_price, ord_stock_cnt, ord_amt, ord_dtm); //주문내역 저장 

                    if (ord_gb == "2") //매수 주문일 경우
                    {
                        update_tb_accnt(ord_gb, ord_amt);
                    }
                } // "if (chejan_gb == "접수")" 종료
                else if (chejan_gb == "체결") //chejan_gb 의 값이 "체결"이라면 체결내역 
                {
                    String user_id = null;
                    String jongmok_cd = null;
                    String jongmok_nm = null;
                    String chegyul_gb = null;
                    int chegyul_no = 0;
                    int chegyul_price = 0;
                    int chegyul_cnt = 0;
                    int chegyul_amt = 0;
                    String chegyul_dtm = null;
                    String ord_no = null;
                    String org_ord_no = null;
                    string ref_dt = null;

                    user_id = g_user_id;
                    jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                    jongmok_nm = get_jongmok_nm(jongmok_cd);
                    chegyul_gb = axKHOpenAPI1.GetChejanData(907).Trim(); //2:매수 1:매도
                    chegyul_no = int.Parse(axKHOpenAPI1.GetChejanData(909).Trim());
                    chegyul_price = int.Parse(axKHOpenAPI1.GetChejanData(910).Trim());
                    chegyul_cnt = int.Parse(axKHOpenAPI1.GetChejanData(911).Trim());
                    chegyul_amt = chegyul_price * chegyul_cnt;
                    org_ord_no = axKHOpenAPI1.GetChejanData(904).Trim();


                    DateTime CurTime;
                    String CurDt;
                    CurTime = DateTime.Now;
                    CurDt = CurTime.ToString("yyyy") + CurTime.ToString("MM") + CurTime.ToString("dd");
                    ref_dt = CurDt;
                    chegyul_dtm = CurDt + axKHOpenAPI1.GetChejanData(908).Trim();
                    ord_no = axKHOpenAPI1.GetChejanData(9203).Trim();

                    write_msg_log("종목코드 : [" + jongmok_cd + "]" + "\n", 0);
                    write_msg_log("종목명 : [" + jongmok_nm + "]" + "\n", 0);
                    write_msg_log("체결구분 : [" + chegyul_gb + "]" + "\n", 0);
                    write_msg_log("체결번호 : [" + chegyul_no.ToString() + "]" + "\n", 0);
                    write_msg_log("체결가격 : [" + chegyul_price.ToString() + "]" + "\n", 0);
                    write_msg_log("체결주식수 : [" + chegyul_cnt.ToString() + "]" + "\n", 0);
                    write_msg_log("체결금액 : [" + chegyul_amt.ToString() + "]" + "\n", 0);
                    write_msg_log("체결일시 : [" + chegyul_dtm + "]" + "\n", 0);
                    write_msg_log("주문번호 : [" + ord_no + "]" + "\n", 0);
                    write_msg_log("원주문번호 : [" + org_ord_no + "]" + "\n", 0);

                    insert_tb_chegyul_lst(ref_dt, jongmok_cd, jongmok_nm, chegyul_gb, chegyul_no, chegyul_price, chegyul_cnt, chegyul_amt, chegyul_dtm, ord_no, org_ord_no); //체결내역 저장 

                    if (chegyul_gb == "1") //매도 체결이라면 계좌 테이블의 매수가능금액을 늘려줌 
                    {
                        update_tb_accnt(chegyul_gb, chegyul_amt);
                    }
                } //else if (chejan_gb == "체결" 종료
            } //if (e.sGubun == "0") 종료
            if (e.sGubun == "1") //sGubun의 값이 "1" 이라면 계좌 정보 
            {
                String user_id = null;
                String jongmok_cd = null;

                int boyu_cnt = 0;
                int boyu_price = 0;
                int boyu_amt = 0;

                user_id = g_user_id;
                jongmok_cd = axKHOpenAPI1.GetChejanData(9001).Trim().Substring(1, 6);
                boyu_cnt = int.Parse(axKHOpenAPI1.GetChejanData(930).Trim());
                boyu_price = int.Parse(axKHOpenAPI1.GetChejanData(931).Trim());
                boyu_amt = int.Parse(axKHOpenAPI1.GetChejanData(932).Trim());

                String l_jongmok_nm = null;
                l_jongmok_nm = get_jongmok_nm(jongmok_cd);

                write_msg_log("종목코드 : [" + jongmok_cd + "]" + "\n", 0);
                write_msg_log("보유주식수 : [" + boyu_cnt.ToString() + "]" + "\n", 0);
                write_msg_log("보유가격 : [" + boyu_price.ToString() + "]" + "\n", 0);
                write_msg_log("보유금액 : [" + boyu_amt.ToString() + "]" + "\n", 0);

                merge_tb_accnt_info(jongmok_cd, l_jongmok_nm, boyu_cnt, boyu_price, boyu_amt); //계좌정보(보유종목) 저장 

            } //if (e.sGubun == "1") 종료
        } //매서드 종료 

        public void insert_tb_ord_lst(string i_ref_dt, String i_jongmok_cd, String i_jongmok_nm, String i_ord_gb, String i_ord_no, String i_org_ord_no, int i_ord_price, int i_ord_stock_cnt, int i_ord_amt, String i_ord_dtm) //주문내역저장 매서드 
        {

            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;


            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //주문내역 저장
            l_sql = @" insert into tb_ord_lst values ( " +
                "'" + g_user_id + "'" + "," +
                "'" + g_accnt_no + "'" + "," +
                "'" + i_ref_dt + "'" + "," +
                "'" + i_jongmok_cd + "'" + "," +
                "'" + i_jongmok_nm + "'" + "," +
                "'" + i_ord_gb + "'" + "," +
                "'" + i_ord_no + "'" + "," +
                "'" + i_org_ord_no + "'" + "," +
                 +i_ord_price + "," +
                 +i_ord_stock_cnt + "," +
                 +i_ord_amt + "," +
                "'" + i_ord_dtm + "'" + "," +
                "'ats'" + "," +
                "SYSDATE" + "," +
                "null" + "," +
                "null" + ") ";

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("insert tb_ord_lst ex : [" + ex.Message + "] \n", 0);
            }
            conn.Close();
        }

        public void update_tb_accnt(String i_chegyul_gb, int i_chegyul_amt) //계좌 테이블 수정 매서드 
        {

            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;

            l_sql = null;

            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            if (i_chegyul_gb == "2") //매수인 경우 주문가능금액에서 체결금액을 빼기
            {
                l_sql = @" update TB_ACCNT set ORD_POSSIBLE_AMT = ord_possible_amt - " + i_chegyul_amt + ", updt_dtm = SYSDATE, updt_id = 'ats' " +
                        " where user_id = " + "'" + g_user_id + "'" +
                        " and accnt_no = " + "'" + g_accnt_no + "'" +
                        " and ref_dt = to_char(sysdate, 'yyyymmdd') ";
            }
            else if (i_chegyul_gb == "1") //매도인 경우 주문가능금액에서 체결금액을 더하기
            {
                l_sql = @" update TB_ACCNT set ORD_POSSIBLE_AMT = ord_possible_amt + " + i_chegyul_amt + ", updt_dtm = SYSDATE, updt_id = 'ats' " +
                        " where user_id = " + "'" + g_user_id + "'" +
                        " and accnt_no = " + "'" + g_accnt_no + "'" +
                        " and ref_dt = to_char(sysdate, 'yyyymmdd') ";
            }

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("update TB_ACCNT ex.Message : [" + ex.Message + "]\n", 0);
            }
            conn.Close();
        }

        public void insert_tb_chegyul_lst(string i_ref_dt, String i_jongmok_cd, String i_jongmok_nm, String i_chegyul_gb, int i_chegyul_no, int i_chegyul_price, int i_chegyul_stock_cnt, int i_chegyul_amt, String i_chegyul_dtm, String i_ord_no, String i_org_ord_no) //체결내역 저장 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;

            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //체결내역 테이블 삽입
            l_sql = @" insert into tb_chegyul_lst values ( " +
                    "'" + g_user_id + "'" + "," +
                    "'" + g_accnt_no + "'" + "," +
                    "'" + i_ref_dt + "'" + "," +
                    "'" + i_jongmok_cd + "'" + "," +
                    "'" + i_jongmok_nm + "'" + "," +
                    "'" + i_chegyul_gb + "'" + "," +
                     "'" + i_ord_no + "'" + "," +
                     "'" + i_chegyul_gb + "'" + "," +
                    +i_chegyul_no + "," +
                    +i_chegyul_price + "," +
                    +i_chegyul_stock_cnt + "," +
                    +i_chegyul_amt + "," +
                    "'" + i_chegyul_dtm + "'" + "," +
                    "'ats'" + "," +
                    "SYSDATE" + "," +
                    "null" + "," +
                    "null" + ") ";

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("insert tb_chegyul_lst ex : [" + ex.Message + "]\n", 0);
            }

            conn.Close();
        }

        public void merge_tb_accnt_info(String i_jongmok_cd, String i_jongmok_nm, int i_boyu_cnt, int i_boyu_price, int i_boyu_amt) //계좌정보 테이블 세팅 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;


            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //계좌정보 테이블 세팅, 기존에 보유중인 종목이면 갱신, 보유하지 않았으면 신규로 삽입 
            l_sql = @"merge into TB_ACCNT_INFO a
                    using (
                      select nvl(max(user_id), '0') user_id, nvl(max(ref_dt), '0') ref_dt, nvl(max(jongmok_cd) , '0') jongmok_cd, nvl(max(jongmok_nm) , '0') jongmok_nm
                      from TB_ACCNT_INFO
                      where user_id = '" + g_user_id + "'" +
                                "and ACCNT_NO = '" + g_accnt_no + "'" +
                                "and jongmok_cd = '" + i_jongmok_cd + "'" +
                                "and ref_dt =  to_char(sysdate, 'yyyymmdd') " +
                           " ) b " +
                           " on ( a.user_id = b.user_id and a.jongmok_cd = b.jongmok_cd and a.ref_dt = b.ref_dt) " +
                           " when matched then update  " +
                           " set OWN_STOCK_CNT = " + i_boyu_cnt + "," +
                           " BUY_PRICE = " + i_boyu_price + "," +
                           " OWN_AMT = " + i_boyu_amt + "," +
                           " updt_dtm = SYSDATE" + "," +
                           " updt_id = 'ats'" +
                           " when not matched then insert (a.user_id,a.accnt_no, a.ref_dt, a.jongmok_cd, a.jongmok_nm,a.BUY_PRICE,a.OWN_STOCK_CNT, a.OWN_AMT, a.inst_dtm, a.inst_id) values ( " +
                                                                    "'" + g_user_id + "'" + "," +
                                                                    "'" + g_accnt_no + "'" + "," +
                                                                      "to_char(sysdate, 'yyyymmdd') , " +
                                                                    "'" + i_jongmok_cd + "'" + "," +
                                                                    "'" + i_jongmok_nm + "'" + "," +
                                                                    +i_boyu_price + "," +
                                                                    +i_boyu_cnt + "," +
                                                                    +i_boyu_amt + "," +
                                                                    "SYSDATE, " +
                                                                    "'ats'" +
                           " ) ";

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("merge TB_ACCNT_INFO ex : [" + ex.Message + "]\n", 0);
            }
            conn.Close();
        }


        int g_flag_1 = 0; //1이면 요청에 대한 응답 완료
        string g_rqname = null; //요청명 변수 
               
        int g_ord_amt_possible = 0; //매수가능금액 

        public void set_tb_accnt() //계좌 테이블 세팅 매서드 
        {
            int l_for_cnt = 0;
            int l_for_flag = 0;

            write_msg_log("TB_ACCNT 테이블 세팅 시작\n", 0);
           
            g_ord_amt_possible = 0; //매수가능금액

            l_for_flag = 0;
            for (;;)
            {
                axKHOpenAPI1.SetInputValue("계좌번호", g_accnt_no);
                axKHOpenAPI1.SetInputValue("비밀번호", "");
                           
                g_rqname = "";
                g_rqname = "증거금세부내역조회요청"; //요청명 정의
                g_flag_1 = 0; //요청중

                String l_scr_no = null; //화면번호를 담을 변수선언
                l_scr_no = "";
                l_scr_no = get_scr_no(); //화면번호 채번
                axKHOpenAPI1.CommRqData("증거금세부내역조회요청", "opw00013", 0, l_scr_no); //OPEN API로 데이터 요청

                l_for_cnt = 0;
                for (;;) //요청후 대기시작
                {
                    if (g_flag_1 == 1) //요청에 대한 응답이 완료되면 루프를 빠져나옴
                    {
                        delay(1000);
                        axKHOpenAPI1.DisconnectRealData(l_scr_no);
                        l_for_flag = 1;
                        break;
                    }
                    else //아직 요청에 대한 응답이 오지 않은 경우
                    {
                        write_msg_log("'증거금세부내역조회요청' 완료 대기중...\n", 0);
                        delay(1000);
                        l_for_cnt++;
                        if (l_for_cnt == 1) //한번이라도 실패하면 루프를 빠져나감(증권계좌 비밀번호 오류방지)
                        {
                            l_for_flag = 0;
                            break;
                        }
                        else
                        {
                            continue;
                        }

                    }
                }
                axKHOpenAPI1.DisconnectRealData(l_scr_no); //화면번호 접속해제

                if (l_for_flag == 1) //요청에 대한 응답을 받았으므로 무한루프에서 빠져나옴
                {
                    break;
                }
                else if (l_for_flag == 0) //요청에 대한 응답을 받지 못해도 비밀번호 5회오류방지를 위해서 무한루프에서 빠져나옴
                {
                    delay(1000);
                    break; //비밀번호 5회 오류 방지
                }
                delay(1000);
            }

            write_msg_log("주문가능금액 : [" + g_ord_amt_possible.ToString() + "]\n", 0);

            merge_tb_accnt(g_ord_amt_possible);                        
        } //set_tb_accnt() 종료

        public void merge_tb_accnt(int g_ord_amt_possible) //계좌 정보 테이블 세팅 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;

            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            if (conn != null)
            {
                cmd = new OracleCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;

                //계좌정보 테이블 세팅
                l_sql = @"merge into tb_accnt a
                            using (
	                                select nvl(max(user_id), ' ') user_id, nvl(max(accnt_no), ' ') accnt_no, nvl(max(ref_dt), ' ') ref_dt " +
                                    " from tb_accnt " +
                                    " where user_id = '" + g_user_id + "'" +
                                    "and accnt_no = " + "'" + g_accnt_no + "'" +
                                    "and ref_dt = to_char(sysdate, 'yyyymmdd') " +
                               " ) b " +
                               " on ( a.user_id = b.user_id and a.accnt_no = b.accnt_no and a.ref_dt = b.ref_dt) " +
                               " when matched then update  " +
                               " set ord_possible_amt = " + g_ord_amt_possible + "," +
                               " updt_dtm = SYSDATE" + "," +
                               " updt_id = 'ats'" +
                               " when not matched then insert (a.user_id, a.accnt_no, a.ref_dt, a.ord_possible_amt, a.inst_dtm, a.inst_id) values ( " +
                                                                        "'" + g_user_id + "'" + "," +
                                                                        "'" + g_accnt_no + "'" + "," +
                                                                        " to_char(sysdate, 'yyyymmdd') " + "," +
                                                                         +g_ord_amt_possible + "," +
                                                                        "SYSDATE, " +
                                                                        "'ats'" +
                               " )";

                cmd.CommandText = l_sql;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    write_err_log("merge_tb_accnt() ex : [" + ex.Message + "]\n", 0);
                }
                finally
                {
                    conn.Close();
                }
            }
            else
            {
                write_msg_log("db connection check!\n", 0);
            }
        }

        int g_flag_2 = 0; //1이면 요청에 대한 응답 완료
        int g_is_next = 0; //다음 조회 데이터가 있는지 확인         

        public void set_tb_accnt_info() //계좌정보 테이블 세팅 
        {
            OracleCommand cmd;
            OracleConnection conn;
            String sql;
            int l_for_cnt = 0;
            int l_for_flag = 0;

            sql = null;
            cmd = null;

            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = @"delete from tb_accnt_info where ref_dt = to_char(sysdate, 'yyyymmdd') and user_id = " + "'" + g_user_id + "'"; //당일기준 계좌정보 삭제 

            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("delete tb_accnt_info ex.Message : [" + ex.Message + "]\n", 0);
            }

            conn.Close();

            g_is_next = 0;            

            for (;;)
            {
                l_for_flag = 0;
                for (;;)
                {
                    axKHOpenAPI1.SetInputValue("계좌번호", g_accnt_no);
                    axKHOpenAPI1.SetInputValue("비밀번호", "");
                    axKHOpenAPI1.SetInputValue("상장폐지조회구분", "1");
                    axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");

                    g_flag_2 = 0;
                    g_rqname = "계좌평가현황요청";

                    String l_scr_no = get_scr_no();

                    //계좌정보 데이터 수신 요청 
                    axKHOpenAPI1.CommRqData("계좌평가현황요청", "OPW00004", g_is_next, l_scr_no); //axKHOpenAPI_OnReceiveTrData 호출

                    l_for_cnt = 0;
                    for (;;)
                    {
                        if (g_flag_2 == 1)
                        {
                            delay(1000);
                            axKHOpenAPI1.DisconnectRealData(l_scr_no);
                            l_for_flag = 1;

                            break;
                        }
                        else
                        {
                            delay(1000);
                            l_for_cnt++;
                            if (l_for_cnt == 5)
                            {
                                l_for_flag = 0;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    delay(1000);
                    axKHOpenAPI1.DisconnectRealData(l_scr_no);

                    if (l_for_flag == 1)
                    {
                        break;
                    }
                    else if (l_for_flag == 0)
                    {
                        delay(1000);
                        continue;
                    }
                }

                if (g_is_next == 0)
                {
                    break;
                }
                delay(1000);
            }
        }

        public void insert_tb_accnt_info(string i_jongmok_cd, string i_jongmok_nm, int i_buy_price, int i_own_stock_cnt, int i_own_amt) //계좌정보 테이블 삽입 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String l_sql = null;


            l_sql = null;
            cmd = null;
            conn = null;
            conn = connect_db();

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            //계좌정보 테이블 삽입 
            l_sql = @" insert into tb_accnt_info values ( " +
                    "'" + g_user_id + "'" + "," +
                    "'" + g_accnt_no + "'" + "," +
                     "to_char(sysdate, 'yyyymmdd')" + "," +
                    "'" + i_jongmok_cd + "'" + "," +
                    "'" + i_jongmok_nm + "'" + "," +
                    +i_buy_price + "," +
                    +i_own_stock_cnt + "," +
                    +i_own_amt + "," +
                    "'ats'" + "," +
                    "SYSDATE" + "," +
                    "null" + "," +
                    "null" + ") ";

            cmd.CommandText = l_sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                write_err_log("insert tb_accnt_info() insert tb_accnt_info ex.Message : [" + ex.Message + "]\n", 0);
            }
            conn.Close();
        }

        public void sell_ord_first() //최초 계좌정보의 보유종목 매도주문 매서드 
        {
            OracleCommand cmd = null;
            OracleConnection conn = null;
            String sql = null;
            OracleDataReader reader = null;

            string l_jongmok_cd = null;
            int l_buy_price = 0;
            int l_own_stock_cnt = 0;
            int l_target_price = 0;

            conn = null;
            conn = connect_db();

            sql = null;
            cmd = null;
            reader = null;

            cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.Text;

            sql = @" SELECT " +
                   "     A.JONGMOK_CD, " +
                   "     A.BUY_PRICE, " +
                   "     A.OWN_STOCK_CNT, " +
                   "     B.TARGET_PRICE " +
                   " FROM TB_ACCNT_INFO A, " +
                   "      TB_TRD_JONGMOK B " +
                   " WHERE A.USER_ID = " + "'" + g_user_id + "'" +
                   " AND A.ACCNT_NO = " + "'" + g_accnt_no + "'" +
                   " AND A.REF_DT = TO_CHAR(SYSDATE, 'YYYYMMDD') " +
                   " AND A.USER_ID = B.USER_ID " +
                   " AND A.JONGMOK_CD = B.JONGMOK_CD " +
                   " AND B.SELL_TRD_YN = 'Y' AND A.OWN_STOCK_CNT > 0 ";

            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                l_jongmok_cd = "";
                l_buy_price = 0;
                l_own_stock_cnt = 0;
                l_target_price = 0;

                l_jongmok_cd = reader[0].ToString().Trim();
                l_buy_price = int.Parse(reader[1].ToString().Trim());
                l_own_stock_cnt = int.Parse(reader[2].ToString().Trim());
                l_target_price = int.Parse(reader[3].ToString().Trim());

                write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                write_msg_log("매입가격 : [" + l_buy_price.ToString() + "]\n", 0);
                write_msg_log("보유주식수 : [" + l_own_stock_cnt.ToString() + "]\n", 0);
                write_msg_log("목표가격 : [" + l_target_price.ToString() + "]\n", 0);

                int l_new_target_price = 0;
                l_new_target_price = get_hoga_unit_price(l_target_price, l_jongmok_cd, 0);

                g_flag_4 = 0;
                g_rqname = "매도주문";

                String l_scr_no = null;
                l_scr_no = "";
                l_scr_no = get_scr_no();

                int ret = 0;

                //매도주문 요청 
                ret = axKHOpenAPI1.SendOrder("매도주문", l_scr_no, g_accnt_no,
                                                2, l_jongmok_cd, l_own_stock_cnt,
                                                l_new_target_price, "00", "");
                if (ret == 0)
                {
                    write_msg_log("매도주문 Sendord() 호출 성공\n", 0);
                    write_msg_log("종목코드 : [" + l_jongmok_cd + "]\n", 0);
                }
                else
                {
                    write_msg_log("매도주문 Sendord() 호출 실패\n", 0);
                    write_msg_log("i_jongmok_cd : [" + l_jongmok_cd + "]\n", 0);
                }

                delay(200);

                for (;;)
                {
                    if (g_flag_4 == 1)
                    {
                        delay(200);
                        axKHOpenAPI1.DisconnectRealData(l_scr_no);
                        break;
                    }
                    else
                    {
                        write_msg_log("'매도주문' 완료 대기중...\n", 0);
                        delay(200);
                        break;
                    }
                }
                axKHOpenAPI1.DisconnectRealData(l_scr_no);
            } //while (reader.Read()) 의 종료
            reader.Close();
            conn.Close();
        } //sell_ord_first 종료

        public int get_hoga_unit_price(int i_price, String i_jongmok_cd, int i_hoga_unit_jump) ///호가가격 가져오기 매서드 
        {
            int l_market_type;
            int l_rest;

            l_market_type = 0;

            try
            {
                l_market_type = int.Parse(axKHOpenAPI1.GetMarketType(i_jongmok_cd).ToString()); //시장구분 가져오기 
            }
            catch (Exception ex)
            {
                write_err_log("get_hoga_unit_price() ex.Message : [" + ex.Message + "]\n", 0);
            }

            if (i_price < 1000)
            {
                return i_price + (i_hoga_unit_jump * 1);
            }
            else if (i_price >= 1000 && i_price < 5000)
            {
                l_rest = i_price % 5;
                if (l_rest == 0)
                {
                    return i_price + (i_hoga_unit_jump * 5);
                }
                else if (l_rest < 3)
                {
                    return (i_price - l_rest) + (i_hoga_unit_jump * 5);
                }
                else
                {
                    return (i_price + (5 - l_rest)) + (i_hoga_unit_jump * 5);
                }
            }
            else if (i_price >= 5000 && i_price < 10000)
            {
                l_rest = i_price % 10;
                if (l_rest == 0)
                {
                    return i_price + (i_hoga_unit_jump * 10);
                }
                else if (l_rest < 5)
                {
                    return (i_price - l_rest) + (i_hoga_unit_jump * 10);
                }
                else
                {
                    return (i_price + (10 - l_rest)) + (i_hoga_unit_jump * 10);
                }
            }
            else if (i_price >= 10000 && i_price < 50000)
            {
                l_rest = i_price % 50;
                if (l_rest == 0)
                {
                    return i_price + (i_hoga_unit_jump * 50);
                }
                else if (l_rest < 25)
                {
                    return (i_price - l_rest) + (i_hoga_unit_jump * 50);
                }
                else
                {
                    return (i_price + (50 - l_rest)) + (i_hoga_unit_jump * 50);
                }
            }
            else if (i_price >= 50000 && i_price < 100000)
            {
                l_rest = i_price % 100;
                if (l_rest == 0)
                {
                    return i_price + (i_hoga_unit_jump * 100);
                }
                else if (l_rest < 50)
                {
                    return (i_price - l_rest) + (i_hoga_unit_jump * 100);
                }
                else
                {
                    return (i_price + (100 - l_rest)) + (i_hoga_unit_jump * 100);
                }
            }
            else if (i_price >= 100000 && i_price < 500000)
            {
                if (l_market_type == 10)
                {
                    l_rest = i_price % 100;
                    if (l_rest == 0)
                    {
                        return i_price + (i_hoga_unit_jump * 100);
                    }
                    else if (l_rest < 50)
                    {
                        return (i_price - l_rest) + (i_hoga_unit_jump * 100);
                    }
                    else
                    {
                        return (i_price + (100 - l_rest)) + (i_hoga_unit_jump * 100);
                    }
                }
                else
                {
                    l_rest = i_price % 500;
                    if (l_rest == 0)
                    {
                        return i_price + (i_hoga_unit_jump * 500);
                    }
                    else if (l_rest < 250)
                    {
                        return (i_price - l_rest) + (i_hoga_unit_jump * 500);
                    }
                    else
                    {
                        return (i_price + (500 - l_rest)) + (i_hoga_unit_jump * 500);
                    }
                }
            }
            else if (i_price >= 500000)
            {
                if (l_market_type == 10)
                {
                    l_rest = i_price % 100;
                    if (l_rest == 0)
                    {
                        return i_price + (i_hoga_unit_jump * 100);
                    }
                    else if (l_rest < 50)
                    {
                        return (i_price - l_rest) + (i_hoga_unit_jump * 100);
                    }
                    else
                    {
                        return (i_price + (100 - l_rest)) + (i_hoga_unit_jump * 100);
                    }
                }
                else
                {
                    l_rest = i_price % 1000;
                    if (l_rest == 0)
                    {
                        return i_price + (i_hoga_unit_jump * 1000);
                    }
                    else if (l_rest < 500)
                    {
                        return (i_price - l_rest) + (i_hoga_unit_jump * 1000);
                    }
                    else
                    {
                        return (i_price + (1000 - l_rest)) + (i_hoga_unit_jump * 1000);
                    }
                }
            }
            return 0;
        }

    }
}
